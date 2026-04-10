using MediatR;
using Microsoft.AspNetCore.Http;
using TicketBooking.Application.Interfaces;

namespace TicketBooking.Application.Commands;

public record UpdateConcertCommand(
    Guid Id,
    string Name,
    DateTime Date,
    IFormFile? PosterFile, // เป็น Nullable เพราะอาจจะไม่เปลี่ยนรูป
    decimal VipPrice,
    decimal GaPrice
) : IRequest<bool>;

public class UpdateConcertCommandHandler : IRequestHandler<UpdateConcertCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public UpdateConcertCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateConcertCommand request, CancellationToken ct)
    {
        var concert = await _context.Concerts.FindAsync(new object[] { request.Id }, ct);
        if (concert == null) return false;

        concert.Name = request.Name;
        concert.Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);

        // ถ้ามีการอัปโหลดรูปใหม่
        if (request.PosterFile != null)
        {
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.PosterFile.FileName)}";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "posters", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.PosterFile.CopyToAsync(stream, ct);
            }
            concert.PosterImageUrl = $"http://localhost:5177/posters/{fileName}";
        }

        // อัปเดตราคาในตาราง Zones (เพื่อให้ตั๋วใหม่ที่จะสร้างใช้ราคานี้ แต่ตั๋วเก่าราคาเดิม)
        var zones = _context.Zones.Where(z => z.ConcertId == concert.Id);
        foreach (var zone in zones)
        {
            if (zone.Name == "VIP") zone.Price = request.VipPrice;
            else if (zone.Name == "GA") zone.Price = request.GaPrice;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}