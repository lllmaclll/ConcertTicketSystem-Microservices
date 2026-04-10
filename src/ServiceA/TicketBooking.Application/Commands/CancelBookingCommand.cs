using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Enums;

namespace TicketBooking.Application.Commands;

public record CancelBookingCommand(Guid TicketId) : IRequest<bool>;

public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITicketLockService _lockService;

    public CancelBookingCommandHandler(IApplicationDbContext context, ITicketLockService lockService)
    {
        _context = context;
        _lockService = lockService;
    }

    public async Task<bool> Handle(CancelBookingCommand request, CancellationToken ct)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        // ถ้ายกเลิกได้ ต้องอยู่ในสถานะ Locked เท่านั้น
        if (ticket == null || ticket.Status != TicketStatus.Locked) 
            return false;

        // 1. คืนสถานะที่นั่งใน Database
        ticket.Status = TicketStatus.Available;
        ticket.UserId = null;
        ticket.CreatedAt = null;

        await _context.SaveChangesAsync(ct);
        
        // 2. ปลดล็อกใน Redis ทันที
        await _lockService.ReleaseLockAsync(ticket.ConcertId, ticket.SeatNumber);

        return true;
    }
}