using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Application.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketBooking.Application.Common.Responses;
using TicketBooking.Application.Common.Dtos;

namespace TicketBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
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
}