using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Application.Common.Dtos;

namespace TicketBooking.Application.Queries;

public record GetMyTicketsQuery(Guid UserId) : IRequest<List<BookingResponseDto>>;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, List<BookingResponseDto>>
{
    private readonly IApplicationDbContext _context;
    public GetMyTicketsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<BookingResponseDto>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        return await _context.Tickets
            .Where(t => t.UserId == request.UserId)
            .Select(t => new BookingResponseDto {
                BookingId = t.Id,
                // ใช้การดึงชื่อแบบนี้แทน ?. เพื่อให้ SQL ทำงานได้
                ConcertName = _context.Concerts
                    .Where(c => c.Id == t.ConcertId)
                    .Select(c => c.Name)
                    .FirstOrDefault() ?? "ไม่พบชื่อคอนเสิร์ต",
                SeatNumber = t.SeatNumber,
                Status = t.Status.ToString(),
                ReservedAt = t.CreatedAt ?? DateTime.UtcNow
            })
            .ToListAsync(ct);
    }
}