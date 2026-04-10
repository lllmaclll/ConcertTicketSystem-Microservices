using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketBooking.Infrastructure.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.Users.AnyAsync())
        {
            context.Users.Add(new User { 
                Id = Guid.Parse("99999999-8888-7777-6666-555555555555"), 
                Username = "tony", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), 
                Email = "tony@example.com",
                Role = "Admin" // 🔥 ตั้งให้เป็น Admin เพื่อทดสอบ Dashboard
            });
            await context.SaveChangesAsync();
        }

        // --- เพิ่มกองทัพ User 1,000 คน สำหรับเทส Load ---
        // --- แก้ไขส่วนสร้างกองทัพ User ให้เร็วขึ้น 100 เท่า ---
        if (await context.Users.CountAsync() < 100)
        {
            // 🔥 หัวใจสำคัญ: Hash รหัสผ่านเตรียมไว้ "ครั้งเดียว" ก่อนเริ่ม Loop
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123");

            var users = new List<User>();
            for (int i = 1; i <= 1000; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"user_{i}",
                    PasswordHash = hashedPassword, // 🔥 ใช้ตัวที่เตรียมไว้
                    Email = $"user_{i}@test.com"
                });
            }
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ สร้างกองทัพ 1,000 Users สำเร็จในพริบตา!");
        }

        if (!await context.Concerts.AnyAsync())
        {
            var concerts = new List<Concert> {
            new Concert { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Bodyslam Live in BKK", Date = DateTime.UtcNow.AddMonths(3), PosterImageUrl = "http://localhost:5177/posters/bodyslam.jpg", TotalSeats = 100 },
            new Concert { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Taylor Swift: The Eras Tour", Date = DateTime.UtcNow.AddMonths(2), PosterImageUrl = "http://localhost:5177/posters/taylor.jpg", TotalSeats = 100 },
            new Concert { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Maroon 5 World Tour", Date = DateTime.UtcNow.AddMonths(4), PosterImageUrl = "http://localhost:5177/posters/maroon5.jpg", TotalSeats = 100 },
            new Concert { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Big Mountain Music Festival", Date = DateTime.UtcNow.AddMonths(5), PosterImageUrl = "http://localhost:5177/posters/bmmf.jpg", TotalSeats = 100 },
            new Concert { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "ห้วยไร่อีหลีน่า Festival", Date = DateTime.UtcNow.AddMonths(1), PosterImageUrl = "http://localhost:5177/posters/huairai.jpg", TotalSeats = 100 }
        };

            foreach (var c in concerts)
            {
                context.Concerts.Add(c);
                var vip = new Zone { Id = Guid.NewGuid(), ConcertId = c.Id, Name = "VIP", Price = 5000, TotalSeats = 40 };
                var ga = new Zone { Id = Guid.NewGuid(), ConcertId = c.Id, Name = "GA", Price = 2000, TotalSeats = 60 };
                context.Zones.AddRange(vip, ga);

                // สร้างที่นั่งแบบสี่เหลี่ยมมาตรฐาน (VIP 4 แถว, GA 6 แถว)
                for (int r = 1; r <= 10; r++)
                {
                    var zone = r <= 4 ? vip : ga;
                    char rowChar = (char)('A' + r - 1);
                    for (int i = 1; i <= 10; i++)
                    {
                        context.Tickets.Add(new Ticket
                        {
                            ConcertId = c.Id,
                            ZoneId = zone.Id,
                            SeatNumber = $"{zone.Name}-{rowChar}{i}", // 🔥 จะได้เป็น VIP-A1, GA-E1
                            Price = zone.Price,
                            Status = TicketStatus.Available
                        });
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}