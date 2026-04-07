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

        // จำลอง Database ในหน่วยความจำ (InMemory) เพื่อความรวดเร็ว
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
        var seat = "A1";

        // สร้างข้อมูลจำลองใน DB ปลอม
        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Bodyslam Test", Date = DateTime.Now });
        _dbContext.Users.Add(new User { Id = userId, Username = "tony", PasswordHash = "hash" });
        await _dbContext.SaveChangesAsync();

        // จำลองว่า Redis Lock ผ่าน (คืนค่า true)
        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = userId };

        // --- Act (สั่งทำงานจริง) ---
        var result = await handler.Handle(command, CancellationToken.None);

        // --- Assert (ตรวจสอบผลลัพธ์) ---
        Assert.NotNull(result);
        Assert.Equal(seat, result.SeatNumber);
        Assert.Equal("Reserved", result.Status);
        Assert.Equal("tony", result.Username); // บรรทัดนี้ต้องผ่านแล้ว!
        
        // ตรวจสอบว่าสั่งส่ง RabbitMQ จริงไหม
        _publisherMock.Verify(x => x.PublishTicketBookedEvent(userId, seat, concertId, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handle_เมื่อล็อกRedisไม่ผ่าน_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        // จำลองว่า Redis ถูกล็อกอยู่โดยคนอื่น (คืนค่า false)
        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = Guid.NewGuid(), SeatNumber = "A1", UserId = Guid.NewGuid() };

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
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => 
            handler.Handle(command, CancellationToken.None));
            
        Assert.Equal("ไม่พบงานคอนเสิร์ตที่คุณเลือก", exception.Message);
    }

    [Fact]
    public async Task Handle_เมื่อที่นั่งถูกขายสำเร็จไปแล้วในฐานข้อมูล_ควรโยนBadRequestException()
    {
        // --- Arrange ---
        var concertId = Guid.NewGuid();
        var seat = "A1";

        _dbContext.Concerts.Add(new Concert { Id = concertId, Name = "Test" });
        // ใส่ตั๋วที่มีสถานะเป็น Booked (ขายแล้ว) ลงใน DB
        _dbContext.Tickets.Add(new Ticket { ConcertId = concertId, SeatNumber = seat, Status = TicketStatus.Booked });
        await _dbContext.SaveChangesAsync();

        _lockServiceMock.Setup(x => x.AcquireLockAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        var handler = new BookTicketCommandHandler(_dbContext, _lockServiceMock.Object, _publisherMock.Object);
        var command = new BookTicketCommand { ConcertId = concertId, SeatNumber = seat, UserId = Guid.NewGuid() };

        // --- Act & Assert ---
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));
    }
}