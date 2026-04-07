using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketBooking.Infrastructure.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. เช็คว่ามี User หรือยัง ถ้ายังไม่มีให้สร้าง User "tony" รหัส "123"
        if (!await context.Users.AnyAsync())
        {
            var testUser = new User
            {
                Id = Guid.Parse("99999999-8888-7777-6666-555555555555"),
                Username = "tony",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), // รหัสผ่านคือ 123
                Email = "tony@example.com"
            };
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        // 2. เช็คว่ามี Concert หรือยัง ถ้ายังไม่มีให้สร้าง
        if (!await context.Concerts.AnyAsync())
        {
            var concertId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            
            var concert = new Concert
            {
                Id = concertId,
                Name = "Bodyslam Live in Bangkok 2026",
                Date = new DateTime(2026, 12, 31, 19, 0, 0, DateTimeKind.Utc),
                PosterImageUrl = "https://example.com/images/bodyslam-poster.jpg",
                TotalSeats = 100 // สมมติว่าขายแค่ 100 ใบ
            };
            context.Concerts.Add(concert);

            // สร้าง Zone VIP และ Zone A
            var vipZone = new Zone { Id = Guid.NewGuid(), ConcertId = concertId, Name = "VIP", Price = 5000, TotalSeats = 20 };
            var zoneA = new Zone { Id = Guid.NewGuid(), ConcertId = concertId, Name = "Zone A", Price = 2500, TotalSeats = 80 };
            context.Zones.AddRange(vipZone, zoneA);

            // สร้างตั๋ว VIP (20 ใบ)
            for (int i = 1; i <= vipZone.TotalSeats; i++)
            {
                context.Tickets.Add(new Ticket
                {
                    ConcertId = concertId,
                    ZoneId = vipZone.Id,
                    SeatNumber = $"VIP-{i}",
                    Price = vipZone.Price,
                    Status = TicketStatus.Available
                });
            }

            // สร้างตั๋ว Zone A (80 ใบ)
            for (int i = 1; i <= zoneA.TotalSeats; i++)
            {
                context.Tickets.Add(new Ticket
                {
                    ConcertId = concertId,
                    ZoneId = zoneA.Id,
                    SeatNumber = $"A-{i}",
                    Price = zoneA.Price,
                    Status = TicketStatus.Available
                });
            }

            await context.SaveChangesAsync();
        }
    }
}