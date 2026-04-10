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
    private readonly ITicketLockService _lockService; // 🔥 เปลี่ยนเป็น Interface ของเรา

    // 🔥 อัปเดต Constructor ให้รับ publisher เข้ามาด้วย
    public ConfirmPaymentCommandHandler(IApplicationDbContext context, IMessagePublisher messagePublisher, ITicketLockService lockService)
    {
        _context = context;
        _messagePublisher = messagePublisher;
        _lockService = lockService;
    }

    public async Task<bool> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. ดึงข้อมูลตั๋ว
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null || ticket.Status != TicketStatus.Locked) return false;

        // 2. หาชื่อคอนเสิร์ต
        var concert = await _context.Concerts.FindAsync(new object?[] { ticket.ConcertId }, cancellationToken);

        // 3. เปลี่ยนสถานะเป็น Booked (จ่ายเงินแล้ว) และบันทึก
        ticket.Status = TicketStatus.Booked;
        await _context.SaveChangesAsync(cancellationToken);

        // 4. 🔥 ลบกุญแจล็อกใน Redis ทันที เพราะจ่ายเงินเสร็จแล้ว ไม่ต้องล็อกแล้ว
        // 🔥 สั่งปลดล็อกผ่าน Interface (โค้ดจะสะอาดและไม่ Error)
        await _lockService.ReleaseLockAsync(ticket.ConcertId, ticket.SeatNumber);

        // 5. เตรียมข้อมูลส่งเมล E-Ticket
        var userId = ticket.UserId ?? Guid.Empty;
        var concertName = concert?.Name ?? "ไม่พบชื่อคอนเสิร์ต";

        await _messagePublisher.PublishPaymentConfirmedEvent(userId, ticket.SeatNumber, concertName, ticket.Id);

        return true;
    }
}