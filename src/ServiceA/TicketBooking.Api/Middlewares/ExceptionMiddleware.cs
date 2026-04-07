using Serilog;
using System.Net;
using TicketBooking.Application.Common.Responses;
using TicketBooking.Application.Common.Exceptions;

namespace TicketBooking.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";

            // --- เริ่มการแยกแยะเคสตรงนี้ ---
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Internal Server Error จากระบบหลังบ้าน";

            if (ex is BadRequestException)
            {
                statusCode = HttpStatusCode.BadRequest; // ตอบ 400
                message = ex.Message; // ใช้ Message ที่ส่งมาจาก Handler จริงๆ
            }
            else
            {
                // ถ้าเป็น Error อื่นๆ ที่ไม่ใช่ BadRequest ให้ Log ไว้ดู (System Error)
                Log.Error(ex, "Unhandled Exception: {Message}", ex.Message);
            }

            context.Response.StatusCode = (int)statusCode;
            var response = ApiResponse<object>.Fail(message);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}