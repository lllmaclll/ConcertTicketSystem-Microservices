using MediatR;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using Microsoft.AspNetCore.Http; // 🔥 เพิ่มอันนี้

namespace TicketBooking.Application.Commands;

public record CreateConcertCommand(
    string Name,
    DateTime Date,
    IFormFile PosterFile, // 🔥 เปลี่ยนจาก string เป็นไฟล์จริง
    decimal VipPrice,
    decimal GaPrice,
    int VipCapacity, // 🔥 เพิ่ม: กำหนดจำนวนที่นั่ง VIP เองได้
    int GaCapacity   // 🔥 เพิ่ม: กำหนดจำนวนที่นั่ง GA เองได้
) : IRequest<Guid>;

public class CreateConcertCommandHandler : IRequestHandler<CreateConcertCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateConcertCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateConcertCommand request, CancellationToken ct)
    {
        // 1. จัดการไฟล์รูปภาพ
        // 🔥 ใช้ Path.GetFileName เพื่อเอาชื่อเดิมของไฟล์ (และตัด path ส่วนเกินถ้ามี)
        string fileName = Path.GetFileName(request.PosterFile.FileName);
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters");

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.PosterFile.CopyToAsync(stream, ct);
        }

        // ใช้ชื่อไฟล์เดิมใน URL
        string imageUrl = $"http://localhost:5177/posters/{fileName}";

        // 1.1 สร้าง Concert (ใช้จำนวนที่นั่งรวมจากที่ส่งมา)
        var concert = new Concert
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            // 🔥 บังคับให้เป็น UTC
            Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
            PosterImageUrl = imageUrl,
            TotalSeats = request.VipCapacity + request.GaCapacity
        };
        _context.Concerts.Add(concert);

        // 2. ใช้ Transaction เพื่อความปลอดภัยของข้อมูล
        using var transaction = await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.BeginTransactionAsync(ct);
        try
        {
            var vipZone = new Zone { Id = Guid.NewGuid(), ConcertId = concert.Id, Name = "VIP", Price = request.VipPrice, TotalSeats = request.VipCapacity };
            var gaZone = new Zone { Id = Guid.NewGuid(), ConcertId = concert.Id, Name = "GA", Price = request.GaPrice, TotalSeats = request.GaCapacity };
            _context.Zones.AddRange(vipZone, gaZone);

            // 🔥 สร้างตั๋ว VIP เท่านั้น
            for (int i = 1; i <= request.VipCapacity; i++)
            {
                _context.Tickets.Add(new Ticket
                {
                    ConcertId = concert.Id,
                    ZoneId = vipZone.Id,
                    SeatNumber = $"VIP-{i}", // ให้ชื่อชัดเจน
                    Price = vipZone.Price,
                    Status = TicketStatus.Available
                });
            }

            // 🔥 สร้างตั๋ว GA เท่านั้น
            for (int i = 1; i <= request.GaCapacity; i++)
            {
                _context.Tickets.Add(new Ticket
                {
                    ConcertId = concert.Id,
                    ZoneId = gaZone.Id,
                    SeatNumber = $"GA-{i}", // ให้ชื่อชัดเจน
                    Price = gaZone.Price,
                    Status = TicketStatus.Available
                });
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return concert.Id;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct); // ถ้าพังให้ดึงข้อมูลกลับทั้งหมด
            throw;
        }
    }
}