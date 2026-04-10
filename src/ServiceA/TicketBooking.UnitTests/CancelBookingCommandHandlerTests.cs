using Microsoft.EntityFrameworkCore;
using Moq;
using TicketBooking.Application.Commands;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Infrastructure.Data;
using Xunit;

namespace TicketBooking.UnitTests;

public class CancelBookingCommandHandlerTests
{
    private readonly Mock<ITicketLockService> _lockServiceMock;
    private readonly ApplicationDbContext _dbContext;

    public CancelBookingCommandHandlerTests()
    {
        _lockServiceMock = new Mock<ITicketLockService>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_เมื่อสถานะเป็นLocked_ควรยกเลิกสำเร็จและปลดล็อกRedis()
    {
        // --- Arrange ---
        var ticketId = Guid.NewGuid();
        var concertId = Guid.NewGuid();
        var ticket = new Ticket { 
            Id = ticketId, 
            ConcertId = concertId, 
            SeatNumber = "A1", 
            Status = TicketStatus.Locked, 
            UserId = Guid.NewGuid() 
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var handler = new CancelBookingCommandHandler(_dbContext, _lockServiceMock.Object);

        // --- Act ---
        var result = await handler.Handle(new CancelBookingCommand(ticketId), default);

        // --- Assert ---
        Assert.True(result);
        var updatedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(TicketStatus.Available, updatedTicket!.Status);
        Assert.Null(updatedTicket.UserId);
        
        // ตรวจสอบว่าต้องสั่งปลดล็อก Redis ด้วย
        _lockServiceMock.Verify(x => x.ReleaseLockAsync(concertId, "A1"), Times.Once);
    }

    [Fact]
    public async Task Handle_เมื่อสถานะเป็นBooked_ต้องห้ามยกเลิก()
    {
        // --- Arrange ---
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket { 
            Id = ticketId, 
            Status = TicketStatus.Booked, // จ่ายเงินแล้ว
            SeatNumber = "A1" 
        };
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var handler = new CancelBookingCommandHandler(_dbContext, _lockServiceMock.Object);

        // --- Act ---
        var result = await handler.Handle(new CancelBookingCommand(ticketId), default);

        // --- Assert ---
        Assert.False(result); // ต้องคืนค่า false (ยกเลิกไม่ได้)
        var updatedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(TicketStatus.Booked, updatedTicket!.Status); // สถานะต้องยังเป็น Booked เหมือนเดิม
    }
}