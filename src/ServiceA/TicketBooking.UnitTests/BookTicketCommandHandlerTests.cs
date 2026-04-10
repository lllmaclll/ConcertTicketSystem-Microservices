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

        // จำลอง Database ในหน่วยความจำ (InMemory)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_เมื่อข้อมูลถูกต้องและที่นั่งว่าง_ควรจองสำเร็จ()
    {
        // --- Arrange (เตรียมข้อมูล) ---
        var concertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seat = "VIP-1";

        // 1. ใส่ข้อมูลคอนเสิร์ตและผู้ใช้
        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Bodyslam Test", Date = DateTime.Now });
        _dbContext.Users.Add(new User { Id = userId, Username = "tony", PasswordHash = "hash" });

        // 🔥 จุดที่เพิ่ม: ต้องสร้าง Zone "VIP" ด้วยเพื่อให้ Code จริงผ่านด่านตรวจโซน
        _dbContext.Zones.Add(new Zone { ConcertId = concertId, Name = "VIP", Price = 5000 });

        await _dbContext.SaveChangesAsync();

        // 2. จำลองว่า Redis Lock ผ่าน
        _lockServiceMock.Setup(x => x.AcquireLockAsync(concertId, seat, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = userId };

        // --- Act (สั่งทำงานจริง) ---
        var result = await handler.Handle(command, CancellationToken.None);

        // --- Assert (ตรวจสอบผลลัพธ์) ---
        Assert.NotNull(result);
        Assert.Equal(seat, result.SeatNumber);
        Assert.Equal("Reserved", result.Status);
        Assert.Equal("tony", result.Username);
        Assert.Equal(5000, result.Price);

        // ตรวจสอบว่ามีการสั่งส่ง RabbitMQ จริง
        _publisherMock.Verify(x => x.PublishTicketBookedEvent(userId, seat, concertId, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handle_เมื่อล็อกRedisไม่ผ่าน_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var seat = "VIP-1";

        _lockServiceMock.Setup(x => x.AcquireLockAsync(concertId, seat, It.IsAny<TimeSpan>()))
            .ReturnsAsync(false); // จำลองว่ามีคนจองอยู่

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = Guid.NewGuid() };

        // --- Act & Assert ---
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("ที่นั่งนี้ถูกจองหรือกำลังมีคนทำรายการอยู่", exception.Message);
    }

    [Fact]
    public async Task Handle_เมื่อไม่พบรหัสคอนเสิร์ต_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = Guid.NewGuid(), SeatNumber = "A1", UserId = Guid.NewGuid() };

        // --- Act & Assert ---
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_เมื่อที่นั่งถูกขายสำเร็จไปแล้วในฐานข้อมูล_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var seat = "A1";

        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Test" });
        _dbContext.Zones.Add(new Zone { ConcertId = concertId, Name = "A", Price = 2000 });
        _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, SeatNumber = seat, Status = TicketStatus.Booked });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = Guid.NewGuid() };

        // --- Act & Assert ---
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_เมื่อจองเกินโควต้า4ใบ_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Quota Test" });

        // สร้างตั๋ว 4 ใบให้ User คนนี้
        for (int i = 1; i <= 4; i++)
        {
            _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, UserId = userId, Status = TicketStatus.Booked, SeatNumber = $"Old-{i}" });
        }
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = "VIP-5", UserId = userId };

        // --- Act & Assert ---
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, default));
        Assert.Contains("เกินโควต้า", exception.Message);

        // ต้องสั่งปลดล็อกใน Redis ด้วย
        _lockServiceMock.Verify(x => x.ReleaseLockAsync(concertId, "VIP-5"), Times.Once);
    }

    [Fact]
    public async Task Handle_เมื่อระบุเลขที่นั่งที่ไม่มีโซนรองรับ_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Zone Test" });
        _dbContext.Zones.Add(new Zone { ConcertId = concertId, Name = "VIP" });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = "GA-1", UserId = Guid.NewGuid() };

        // --- Act & Assert ---
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, default));
        Assert.Equal("รูปแบบที่นั่งหรือโซนไม่ถูกต้อง", exception.Message);
    }
}