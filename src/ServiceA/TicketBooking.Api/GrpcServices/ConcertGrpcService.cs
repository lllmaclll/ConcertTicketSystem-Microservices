using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Grpc;
using TicketBooking.Application.Interfaces;

namespace TicketBooking.Api.GrpcServices;

public class ConcertGrpcService : ConcertInfo.ConcertInfoBase
{
    private readonly IApplicationDbContext _dbContext;

    public ConcertGrpcService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<ConcertReply> GetConcertDetail(ConcertRequest request, ServerCallContext context)
    {
        var concertId = Guid.Parse(request.ConcertId);

        // ดึงจาก DB จริง
        var concert = await _dbContext.Concerts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == concertId);

        return new ConcertReply
        {
            ConcertName = concert?.Name ?? "ไม่พบชื่อคอนเสิร์ต",
            ConcertDate = concert?.Date.ToString("dd MMM yyyy HH:mm") ?? ""
        };
    }
}