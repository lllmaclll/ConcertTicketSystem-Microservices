using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- ตั้งค่ากฎการจำกัด Request (Rate Limit Policy) ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("booking-limit", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10); // ในเวลา 10 วินาที
        opt.PermitLimit = 3;                  // ให้ยิงได้แค่ 3 ครั้ง
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // ถ้าโดนบล็อก ให้ตอบกลับเป็น 429 Too Many Requests
    // options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // 🔥 แก้ไขตรงนี้: แทนที่จะพ่นแค่ Code ให้พ่น JSON สวยๆ ออกไป
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new {
            success = false,
            message = "คุณยิงคำขอมากเกินไป กรุณารอสักครู่ (Rate Limit Exceeded)",
            data = (object)null
        });
    };
});

// อ่านตั้งค่าแผนที่จาก appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString();
    // ใส่รหัสลงใน Header เพื่อส่งต่อให้ Service ถัดไป
    context.Request.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    await next();
});

// app.MapGet("/", () => "Hello World!");

// --- เปิดใช้งาน Rate Limiter Middleware ---
app.UseRateLimiter();

// เปิดใช้งานระบบส่งต่อ (Reverse Proxy)
app.MapReverseProxy();

app.Run();
