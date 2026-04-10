using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Application.Common.Exceptions;

namespace TicketBooking.Application.Queries;

// 1. ตัวเก็บคำสั่ง (Query)
public record GetConcertDetailsQuery(Guid ConcertId) : IRequest<object>;

// 2. ตัวประมวลผล (Handler)
public class GetConcertDetailsQueryHandler : IRequestHandler<GetConcertDetailsQuery, object>
{
    private readonly IApplicationDbContext _context;

    public GetConcertDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> Handle(GetConcertDetailsQuery request, CancellationToken ct)
    {
        // ดึงข้อมูลคอนเสิร์ต
        var concert = await _context.Concerts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConcertId, ct);

        // 🔥 ถ้าไม่เจอคอนเสิร์ต ให้โยน Error ทันที
        if (concert == null) 
            throw new BadRequestException("ไม่พบข้อมูลคอนเสิร์ตที่ระบุ");

        // ดึงโซนทั้งหมดของคอนเสิร์ตนี้
        var zones = await _context.Zones
            .AsNoTracking()
            .Where(z => z.ConcertId == request.ConcertId)
            .ToListAsync(ct);

        // ดึงที่นั่งทั้งหมด (ตั๋ว) ของคอนเสิร์ตนี้
        var seats = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.ConcertId == request.ConcertId)
            // 🔥 สเต็ปการเรียงแบบอัจฉริยะ:
            // 1. เรียงตามตัวอักษรตัวแรก (แถว A, B, C, D)
            .OrderBy(t => t.SeatNumber.Substring(0, 1)) 
            // 2. เรียงตามความยาวของชื่อ (เพื่อให้ A2 มาก่อน A10)
            .ThenBy(t => t.SeatNumber.Length) 
            // 3. เรียงตามชื่อที่นั่ง (เพื่อให้ A1 มาก่อน A2)
            .ThenBy(t => t.SeatNumber) 
            .ToListAsync(ct);

        return new 
        { 
            concert, 
            zones, 
            seats 
        };
    }
}