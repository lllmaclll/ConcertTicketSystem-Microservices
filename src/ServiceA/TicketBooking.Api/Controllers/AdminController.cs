using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Application.Commands;
using TicketBooking.Application.Common.Responses;

namespace TicketBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // 🔒 ล็อกไว้เฉพาะ Admin เท่านั้น!!
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpPost("concerts")]
    public async Task<IActionResult> CreateConcert([FromForm] CreateConcertCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "สร้างคอนเสิร์ตและที่นั่งสำเร็จ"));
    }

    [HttpDelete("concerts/{id}")]
    public async Task<IActionResult> DeleteConcert(Guid id)
    {
        // 🔥 ระบุประเภท bool ให้ชัดเจนเพื่อแก้ปัญหา Type Mismatch
        bool result = await _mediator.Send(new DeleteConcertCommand(id));
        
        if (result)
            return Ok(ApiResponse<object>.Ok(null, "ลบคอนเสิร์ตสำเร็จ"));
            
        return NotFound(ApiResponse<object>.Fail("ไม่พบคอนเสิร์ตที่ต้องการลบ"));
    }
}