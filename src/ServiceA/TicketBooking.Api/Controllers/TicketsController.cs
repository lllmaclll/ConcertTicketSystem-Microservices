using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketBooking.Application.Common.Responses;
using TicketBooking.Application.Common.Dtos;
using TicketBooking.Application.Queries; // 🔥 เพิ่มเพื่อให้รู้จัก GetConcertDetailsQuery และ GetConcertsQuery
using TicketBooking.Domain.Entities;     // 🔥 เพื่อให้รู้จัก Concert
using TicketBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TicketBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context; // 🔥 เพิ่มตัวแปรนี้

    public TicketsController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context; // 🔥 Assign ค่า
    }

    [HttpPost("book")]
    [Authorize] // 🔒 ล็อก API นี้! ต้องมี Token เท่านั้นถึงจะเข้าได้
    public async Task<IActionResult> BookTicket([FromBody] BookTicketCommand command)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        command.UserId = Guid.Parse(userIdString);

        // ได้ผลลัพธ์เป็น DTO ก้อนใหญ่ที่มีข้อมูลครบ
        var result = await _mediator.Send(command);

        return Ok(ApiResponse<BookingResponseDto>.Ok(result, "จองที่นั่งสำเร็จ โปรดชำระเงินในเวลาที่กำหนด"));
    }

    [HttpPost("confirm-payment/{id}")]
    [Authorize]
    public async Task<IActionResult> ConfirmPayment(Guid id)
    {
        var result = await _mediator.Send(new ConfirmPaymentCommand(id));
        return result ? Ok(ApiResponse<object>.Ok(null, "ชำระเงินสำเร็จ! ตั๋วของคุณพร้อมใช้งานแล้ว")) : BadRequest(ApiResponse<object>.Fail("ไม่สามารถยืนยันการชำระเงินได้"));
    }

    [HttpPost("webhook/payment-success/{id}")]
    public async Task<IActionResult> PaymentWebhook(Guid id)
    {
        // เราจะใช้ Command เดิม (ConfirmPaymentCommand) 
        // แต่ Webhook นี้จำลองว่าเรียกมาจากระบบภายนอก (เช่น Stripe/Omise)
        var result = await _mediator.Send(new ConfirmPaymentCommand(id));

        if (result)
            return Ok(ApiResponse<object>.Ok(null, "ได้รับข้อมูลการชำระเงิน และส่ง E-Ticket เรียบร้อยแล้ว"));

        return BadRequest(ApiResponse<object>.Fail("รหัสการจองไม่ถูกต้อง หรือตั๋วไม่ได้อยู่ในสถานะรอชำระเงิน"));
    }

    [HttpGet("concerts")]
    public async Task<IActionResult> GetConcerts()
    {
        var concerts = await _mediator.Send(new GetConcertsQuery());
        return Ok(ApiResponse<List<Concert>>.Ok(concerts));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(Guid id)
    {
        var details = await _mediator.Send(new GetConcertDetailsQuery(id));
        return Ok(ApiResponse<object>.Ok(details));
    }

    [HttpGet("my-bookings")]
    [Authorize]
    public async Task<IActionResult> GetMyBookings()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var result = await _mediator.Send(new GetMyTicketsQuery(Guid.Parse(userIdString)));
        return Ok(ApiResponse<List<BookingResponseDto>>.Ok(result));
    }

    [HttpGet("booking/{id}")]
    [Authorize]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        // ใช้ AsNoTracking เพื่อความเร็วและดึงข้อมูลที่สดใหม่ที่สุด
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound(ApiResponse<object>.Fail("ไม่พบข้อมูลการจอง หรือการจองหมดอายุแล้ว"));

        var concert = await _context.Concerts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ticket.ConcertId);

        var result = new BookingResponseDto
        {
            BookingId = ticket.Id,
            ConcertName = concert?.Name ?? "ไม่พบชื่อคอนเสิร์ต",
            SeatNumber = ticket.SeatNumber,
            Price = ticket.Price,
            Status = ticket.Status.ToString(),
            ReservedAt = ticket.CreatedAt ?? DateTime.UtcNow
        };

        return Ok(ApiResponse<BookingResponseDto>.Ok(result));
    }

    [HttpPost("cancel/{id}")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _mediator.Send(new CancelBookingCommand(id));
        return result ? Ok(ApiResponse<object>.Ok(null, "ยกเลิกสำเร็จ")) : BadRequest(ApiResponse<object>.Fail("ไม่สามารถยกเลิกการจองได้"));
    }
}