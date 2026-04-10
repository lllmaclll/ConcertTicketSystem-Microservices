using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Enums;
using System.Linq; // 🔥 ต้องมีอันนี้เพื่อให้ใช้ .Where ได้

namespace TicketBooking.Application.Commands;

// กำหนดให้ Command นี้คืนค่าเป็น bool
public record DeleteConcertCommand(Guid Id) : IRequest<bool>;

public class DeleteConcertCommandHandler : IRequestHandler<DeleteConcertCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteConcertCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteConcertCommand request, CancellationToken ct)
    {
        // 1. ค้นหาคอนเสิร์ต
        var concert = await _context.Concerts.FindAsync(new object[] { request.Id }, ct);
        if (concert == null) return false;

        // 2. ค้นหาข้อมูลที่เกี่ยวข้องเพื่อลบทิ้งให้หมด (ป้องกัน Foreign Key Error)
        var tickets = _context.Tickets.Where(t => t.ConcertId == request.Id);
        var zones = _context.Zones.Where(z => z.ConcertId == request.Id);
        
        _context.Tickets.RemoveRange(tickets);
        _context.Zones.RemoveRange(zones);
        _context.Concerts.Remove(concert);

        // 3. บันทึกการลบ
        await _context.SaveChangesAsync(ct);
        return true;
    }
}