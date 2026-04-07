using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Enums;

namespace TicketBooking.Infrastructure.BackgroundServices;

public class TicketCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TicketCleanupService> _logger;

    public TicketCleanupService(IServiceProvider services, ILogger<TicketCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("🧹 กำลังตรวจสอบตั๋วที่หมดเวลาจ่ายเงิน...");

            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                
                // ค้นหาตั๋วที่สถานะเป็น Locked และเวลาผ่านไปเกิน 15 นาที
                var expiryTime = DateTime.UtcNow.AddMinutes(-15);
                
                var expiredTickets = await context.Tickets
                    .Where(t => t.Status == TicketStatus.Locked && t.CreatedAt < expiryTime)
                    .ToListAsync();

                if (expiredTickets.Any())
                {
                    foreach (var ticket in expiredTickets)
                    {
                        ticket.Status = TicketStatus.Available;
                        ticket.UserId = null;
                        _logger.LogWarning($"♻️ คืนที่นั่ง {ticket.SeatNumber} เข้าสู่ระบบเนื่องจากหมดเวลา");
                    }
                    await context.SaveChangesAsync(stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // ทำงานทุกๆ 1 นาที
        }
    }
}