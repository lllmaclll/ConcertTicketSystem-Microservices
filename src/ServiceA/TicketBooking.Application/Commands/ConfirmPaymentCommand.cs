using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Enums;

namespace TicketBooking.Application.Commands;

public record ConfirmPaymentCommand(Guid TicketId) : IRequest<bool>;

public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IMessagePublisher _messagePublisher;

    // 🔥 อัปเดต Constructor ให้รับ publisher เข้ามาด้วย
    public ConfirmPaymentCommandHandler(IApplicationDbContext context, IMessagePublisher messagePublisher)
    {
        _context = context;
        _messagePublisher = messagePublisher;
    }

    public async Task<bool> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. ดึงข้อมูลตั๋ว (ลบ .Include ออกไปเลยครับ)
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null || ticket.Status != TicketStatus.Locked) return false;

        // 2. หาชื่อคอนเสิร์ตจาก ID ที่อยู่ในตั๋ว
        var concert = await _context.Concerts.FindAsync(new object?[] { ticket.ConcertId }, cancellationToken);

        // 3. เปลี่ยนสถานะและบันทึก
        ticket.Status = TicketStatus.Booked;
        await _context.SaveChangesAsync(cancellationToken);

        // 4. เตรียมข้อมูลส่งเมล
        var userId = ticket.UserId ?? Guid.Empty;
        var concertName = concert?.Name ?? "ไม่พบชื่อคอนเสิร์ต";

        // 5. ส่ง Event ไปหา Node.js (Service B)
        await _messagePublisher.PublishPaymentConfirmedEvent(userId, ticket.SeatNumber, concertName, ticket.Id);

        return true;
    }
}