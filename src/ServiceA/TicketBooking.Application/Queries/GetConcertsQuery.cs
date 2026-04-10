using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;

namespace TicketBooking.Application.Queries;

public record GetConcertsQuery() : IRequest<List<Concert>>;

public class GetConcertsQueryHandler : IRequestHandler<GetConcertsQuery, List<Concert>>
{
    private readonly IApplicationDbContext _context;

    public GetConcertsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Concert>> Handle(GetConcertsQuery request, CancellationToken cancellationToken)
    {
        // ดึงรายชื่อคอนเสิร์ตทั้งหมดจาก DB จริง
        return await _context.Concerts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}