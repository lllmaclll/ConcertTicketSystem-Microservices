using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Application.Common.Dtos;
using TicketBooking.Application.Common.Exceptions;
using System.Text.Json.Serialization;

namespace TicketBooking.Application.Commands; // สังเกตว่าเราลบ using Infrastructure ออกไปแล้ว

// 1. ตัวเก็บข้อมูลที่ส่งมาจาก API (Input)
public class BookTicketCommand : IRequest<BookingResponseDto>
{
    /// <example>11111111-2222-3333-4444-555555555555</example>
    public Guid ConcertId { get; set; }

    /// <example>VIP-1</example>
    public string SeatNumber { get; set; } = string.Empty;

    [JsonIgnore] // 🔥 เพิ่มบรรทัดนี้ เพื่อบอก Scalar ว่าไม่ต้องโชว์ฟิลด์นี้ใน API
    public Guid UserId { get; set; }
}

// 2. ตัวประมวลผล (Handler)
public class BookTicketCommandHandler : IRequestHandler<BookTicketCommand, BookingResponseDto>
{
    // เปลี่ยนมาใช้ Interface IApplicationDbContext แทน
    private readonly IApplicationDbContext _dbContext;
    private readonly ITicketLockService _lockService;
    private readonly IMessagePublisher _messagePublisher;

    public BookTicketCommandHandler(IApplicationDbContext dbContext, ITicketLockService lockService, IMessagePublisher messagePublisher)
    {
        _dbContext = dbContext;
        _lockService = lockService;
        _messagePublisher = messagePublisher; // รับค่ามา
    }

    public async Task<BookingResponseDto> Handle(BookTicketCommand request, CancellationToken cancellationToken)
    {
        // 1. ลองล็อกที่นั่งใน Redis (Distributed Lock)
        bool isLocked = await _lockService.AcquireLockAsync(request.ConcertId, request.SeatNumber, TimeSpan.FromMinutes(15));
        if (!isLocked) throw new BadRequestException("ไม่สามารถจองได้: ที่นั่งนี้ถูกจองหรือกำลังมีคนทำรายการอยู่");

        // 2. ตรวจสอบโควต้าการจอง (🔥 กฎเหล็กข้อที่ 1: ห้ามเกิน 4 ใบ)
        var userTicketCount = await _dbContext.Tickets
            .CountAsync(t => t.ConcertId == request.ConcertId
                        && t.UserId == request.UserId
                        && (t.Status == TicketStatus.Locked || t.Status == TicketStatus.Booked), cancellationToken);

        if (userTicketCount >= 4)
        {
            // ถ้าเกินโควต้า ต้องปลดล็อก Redis ที่เพิ่งล็อกไปด้วยครับ
            await _lockService.ReleaseLockAsync(request.ConcertId, request.SeatNumber);
            throw new BadRequestException("คุณจองตั๋วเกินโควต้าที่กำหนด (สูงสุด 4 ใบต่อคอนเสิร์ต)");
        }

        // 3. ตรวจสอบว่าคอนเสิร์ตมีอยู่จริงไหม
        var concert = await _dbContext.Concerts.FindAsync(new object[] { request.ConcertId }, cancellationToken);
        if (concert == null) throw new BadRequestException("ไม่พบงานคอนเสิร์ตที่คุณเลือก");

        // 6. ตรวจสอบข้อมูลผู้ใช้ (เพื่อให้ได้ Username มาใส่ใน DTO)
        var user = await _dbContext.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) throw new BadRequestException("ไม่พบข้อมูลผู้ใช้งาน");

        // 7. ตรวจสอบสถานะตั๋วใน Database (เผื่อกรณีที่มีคนจ่ายเงินสำเร็จไปแล้ว)
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.ConcertId == request.ConcertId && t.SeatNumber == request.SeatNumber, cancellationToken);

        if (ticket == null)
        {
            await _lockService.ReleaseLockAsync(request.ConcertId, request.SeatNumber);
            throw new BadRequestException("ไม่พบข้อมูลที่นั่งนี้ในระบบ");
        }

        // 4. ตรวจสอบโซน (🔥 กฎเหล็กข้อที่ 2: เช็คว่าที่นั่งตรงกับโซนที่มีจริงไหม)
        // ค้นหาข้อมูลโซนเพื่อเอาราคา
        // หาข้อมูล Zone ก่อนเพื่อเอาราคา (ควรหาจาก SeatNumber ว่าอยู่ในโซนไหน หรือส่ง ZoneId มา)
        // เพื่อความง่ายในตอนนี้ ให้หา Zone ที่ชื่อขึ้นต้นตรงกับ SeatNumber เช่น "A-1" อยู่ "Zone A"
        var zone = await _dbContext.Zones.FindAsync(new object[] { ticket.ZoneId }, cancellationToken);

        if (zone == null)
        {
            await _lockService.ReleaseLockAsync(request.ConcertId, request.SeatNumber);
            throw new BadRequestException("ข้อมูลโซนของที่นั่งนี้ไม่ถูกต้อง");
        }

        // 8. บันทึกข้อมูลการจอง (Update หรือ Insert)
        if (ticket == null)
        {
            ticket = new Ticket
            {
                ConcertId = request.ConcertId,
                SeatNumber = request.SeatNumber,
                Status = TicketStatus.Locked,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                ZoneId = zone?.Id ?? Guid.Empty,
                Price = zone?.Price ?? 0
            };
            _dbContext.Tickets.Add(ticket);
        }
        else
        {
            ticket.Status = TicketStatus.Locked;
            ticket.UserId = request.UserId;
            ticket.CreatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 9. ส่ง Message เข้าคิว RabbitMQ เพื่อส่งอีเมลยืนยันชั่วคราว
        await _messagePublisher.PublishTicketBookedEvent(request.UserId, request.SeatNumber, request.ConcertId, ticket.Id);

        // 10. ส่งข้อมูลตอบกลับแบบละเอียด (DTO)
        return new BookingResponseDto
        {
            BookingId = ticket.Id,
            UserId = user.Id,           // 🔥 เพิ่มตรงนี้
            Username = user.Username,   // 🔥 เพิ่มตรงนี้ (เพื่อให้ Test ผ่าน)
            ConcertName = concert.Name,
            SeatNumber = ticket.SeatNumber,
            Price = ticket.Price,        // 🔥 เพิ่ม
            Status = "Reserved",
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }
}