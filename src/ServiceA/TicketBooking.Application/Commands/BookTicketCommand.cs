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

        // 2. ตรวจสอบว่าคอนเสิร์ตมีอยู่จริงไหม
        var concert = await _dbContext.Concerts.FindAsync(new object[] { request.ConcertId }, cancellationToken);
        if (concert == null) throw new BadRequestException("ไม่พบงานคอนเสิร์ตที่คุณเลือก");

        // 3. ตรวจสอบข้อมูลผู้ใช้ (เพื่อให้ได้ Username มาใส่ใน DTO)
        var user = await _dbContext.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) throw new BadRequestException("ไม่พบข้อมูลผู้ใช้งาน");

        // 4. ตรวจสอบสถานะตั๋วใน Database (เผื่อกรณีที่มีคนจ่ายเงินสำเร็จไปแล้ว)
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.ConcertId == request.ConcertId && t.SeatNumber == request.SeatNumber, cancellationToken);

        if (ticket != null && ticket.Status != TicketStatus.Available)
            throw new BadRequestException("ไม่สามารถจองได้: ที่นั่งนี้ถูกขายไปแล้ว");

        // ค้นหาข้อมูลโซนเพื่อเอาราคา
        // หาข้อมูล Zone ก่อนเพื่อเอาราคา (ควรหาจาก SeatNumber ว่าอยู่ในโซนไหน หรือส่ง ZoneId มา)
        // เพื่อความง่ายในตอนนี้ ให้หา Zone ที่ชื่อขึ้นต้นตรงกับ SeatNumber เช่น "A-1" อยู่ "Zone A"
        var zone = await _dbContext.Zones
            .FirstOrDefaultAsync(z => request.SeatNumber.StartsWith(z.Name), cancellationToken);

        // 5. บันทึกข้อมูลการจอง (Update หรือ Insert)
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

         // 6. ส่ง Message เข้าคิว RabbitMQ เพื่อส่งอีเมลยืนยันชั่วคราว
        await _messagePublisher.PublishTicketBookedEvent(request.UserId, request.SeatNumber, request.ConcertId, ticket.Id);

        // 7. ส่งข้อมูลตอบกลับแบบละเอียด (DTO)
        return new BookingResponseDto
        {
            BookingId = ticket.Id,
            UserId = user.Id,           // 🔥 เพิ่มตรงนี้
            Username = user.Username,   // 🔥 เพิ่มตรงนี้ (เพื่อให้ Test ผ่าน)
            ConcertName = concert.Name,
            SeatNumber = ticket.SeatNumber,
            Status = "Reserved",
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }
}