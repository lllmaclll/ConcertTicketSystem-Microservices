using Microsoft.EntityFrameworkCore;
using Moq;
using TicketBooking.Application.Commands;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Domain.Enums;
using TicketBooking.Infrastructure.Data;
using TicketBooking.Application.Common.Exceptions;
using TicketBooking.Application.Common.Dtos;
using Xunit;

namespace TicketBooking.UnitTests;

public class BookTicketCommandHandlerTests
{
    private readonly Mock<ITicketLockService> _lockServiceMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly ApplicationDbContext _dbContext;

    public BookTicketCommandHandlerTests()
    {
        _lockServiceMock = new Mock<ITicketLockService>();
        _publisherMock = new Mock<IMessagePublisher>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_เมื่อข้อมูลถูกต้องและที่นั่งว่าง_ควรจองสำเร็จ()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var seat = "VIP-1";

        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Test", Date = DateTime.Now });
        _dbContext.Users.Add(new User { Id = userId, Username = "tony", PasswordHash = "hash" });
        _dbContext.Zones.Add(new Zone { Id = zoneId, ConcertId = concertId, Name = "VIP", Price = 5000 });
        
        // 🔥 ต้องเพิ่มตั๋วลงไปใน DB จำลองด้วยเพื่อให้ Handler หาเจอ
        _dbContext.Tickets.Add(new Ticket { 
            ConcertId = concertId, 
            ZoneId = zoneId, 
            SeatNumber = seat, 
            Status = TicketStatus.Available 
        });
        
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(concertId, seat, It.IsAny<TimeSpan>())).ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = userId };

        // --- Act ---
        var result = await handler.Handle(command, CancellationToken.None);

        // --- Assert ---
        Assert.NotNull(result);
        Assert.Equal("tony", result.Username);
    }

    [Fact]
    public async Task Handle_เมื่อระบุเลขที่นั่งที่ไม่มีโซนรองรับ_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seat = "GA-1";

        // เตรียมข้อมูลพื้นฐานให้ครบ (Concert, User, Ticket) แต่ให้ Zone เป็น null
        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Test" });
        _dbContext.Users.Add(new User { Id = userId, Username = "testuser" });
        _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, SeatNumber = seat, ZoneId = Guid.NewGuid() }); // ZoneId มั่วๆ
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = userId };

        // --- Act & Assert ---
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, default));
        Assert.Equal("ข้อมูลโซนของที่นั่งนี้ไม่ถูกต้อง", exception.Message);
    }

    [Fact]
    public async Task Handle_เมื่อล็อกRedisไม่ผ่าน_ควรโยนBadRequestException()
    {
        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(false);
        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new BookTicketCommand(), default));
    }

    [Fact]
    public async Task Handle_เมื่อไม่พบรหัสคอนเสิร์ต_ควรโยนBadRequestException()
    {
        var concertId = Guid.NewGuid();
        var seat = "A1";
        _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, SeatNumber = seat }); // มีตั๋วแต่ไม่มีคอนเสิร์ต
        _dbContext.Zones.Add(new Zone { ConcertId = concertId });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new BookTicketCommand { ConcertId = concertId, SeatNumber = seat }, default));
    }

    [Fact]
    public async Task Handle_เมื่อที่นั่งถูกขายสำเร็จไปแล้วในฐานข้อมูล_ควรโยนBadRequestException()
    {
        var concertId = Guid.NewGuid();
        var seat = "A1";
        _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, SeatNumber = seat, Status = TicketStatus.Booked });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new BookTicketCommand { ConcertId = concertId, SeatNumber = seat }, default));
    }

    [Fact]
    public async Task Handle_เมื่อจองเกินโควต้า4ใบ_ควรโยนBadRequestException()
    {
        var concertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        for (int i = 1; i <= 4; i++) _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, UserId = userId, Status = TicketStatus.Booked, SeatNumber = $"S{i}" });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new BookTicketCommand { ConcertId = concertId, UserId = userId }, default));
    }
}