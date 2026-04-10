using Microsoft.EntityFrameworkCore;
using Moq;
using TicketBooking.Application.Commands;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Infrastructure.Data;
using Xunit;

namespace TicketBooking.UnitTests;

public class ConfirmPaymentCommandHandlerTests
{
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<ITicketLockService> _lockServiceMock;
    private readonly ApplicationDbContext _dbContext;

    public ConfirmPaymentCommandHandlerTests()
    {
        _publisherMock = new Mock<IMessagePublisher>();
        _lockServiceMock = new Mock<ITicketLockService>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_เมื่อจ่ายเงินสำเร็จ_ควรเปลี่ยนสถานะเป็นBookedและส่งETicket()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Live Show" });
        _dbContext.Tickets.Add(new Ticket { 
            Id = ticketId, 
            ConcertId = concertId, 
            Status = TicketStatus.Locked, 
            UserId = userId,
            SeatNumber = "B1" 
        });
        await _dbContext.SaveChangesAsync();

        var handler = new ConfirmPaymentCommandHandler(_dbContext, _publisherMock.Object, _lockServiceMock.Object);

        // --- Act ---
        var result = await handler.Handle(new ConfirmPaymentCommand(ticketId), default);

        // --- Assert ---
        Assert.True(result);
        var updatedTicket = await _dbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(TicketStatus.Booked, updatedTicket!.Status);

        // ตรวจสอบว่าสั่งส่ง E-Ticket ผ่าน RabbitMQ หรือไม่
        _publisherMock.Verify(x => x.PublishPaymentConfirmedEvent(userId, "B1", "Live Show", ticketId), Times.Once);
        
        // ตรวจสอบว่าสั่งลบ Lock ใน Redis หรือไม่
        _lockServiceMock.Verify(x => x.ReleaseLockAsync(concertId, "B1"), Times.Once);
    }
}