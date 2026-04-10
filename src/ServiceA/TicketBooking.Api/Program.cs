using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Text;
using TicketBooking.Api.GrpcServices;
using TicketBooking.Api.Middlewares;
using TicketBooking.Application.Commands;
using TicketBooking.Application.Interfaces;
using TicketBooking.Infrastructure.Data;
using TicketBooking.Infrastructure.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using TicketBooking.Infrastructure.BackgroundServices;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TicketBooking.Api.Controllers; // 🔥 เพิ่มเพื่อให้รู้จัก UserDto
// using TicketBooking.Application.Commands; // 🔥 เพิ่มเพื่อให้รู้จัก BookTicketCommand
using MediatR; // 🔥 แก้ Error: IPipelineBehavior
using FluentValidation; // 🔥 แก้ Error: AddValidatorsFromAssembly

var builder = WebApplication.CreateBuilder(args);

// --- เพิ่มเพื่อให้ RabbitMQPublisher เข้าถึง Header ได้ ---
// --- เพิ่มเพื่อให้ระบบสามารถเข้าถึง HttpContext ในชั้นอื่นได้ ---
builder.Services.AddHttpContextAccessor();

// --- Serilog Setup ---
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext() // 🔥 เพิ่มอันนี้เพื่อให้ Log จำรหัสติดตามได้
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day) // 🔥 เก็บเป็นไฟล์รายวัน
    .CreateLogger();
builder.Host.UseSerilog(); // บอกให้ระบบใช้ Serilog แทนตัวเก่า

// --- Kestrel gRPC Setup ---
// บังคับ Kestrel ให้รองรับ HTTP/2 แบบไม่ใช้ TLS (สำคัญที่สุดเพื่อแก้ ECONNRESET)
builder.WebHost.ConfigureKestrel(options =>
{
    // เปลี่ยนจาก ListenLocalhost เป็น ListenAnyIP (พอร์ต 0.0.0.0) เพื่อให้สามารถรับการเชื่อมต่อจากภายนอกได้
    // พอร์ต 5018: สำหรับ Web API และ Scalar (ใช้ HTTP/1)
    options.ListenAnyIP(5018, o => o.Protocols = HttpProtocols.Http1);

    // พอร์ต 5019: สำหรับ gRPC โดยเฉพาะ (ใช้ HTTP/2 เท่านั้น ไม่ต้องมี HTTPS)
    options.ListenAnyIP(5019, o => o.Protocols = HttpProtocols.Http2);
});

// --- Services Setup ---
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // 🔥 ปิดการดัก Error 400 อัตโนมัติ เพื่อให้ไหลไปถึง Middleware ของเรา
        options.SuppressModelStateInvalidFilter = true;
    }); // เปิดใช้ระบบ Controller สำหรับสร้าง API
builder.Services.AddEndpointsApiExplorer();
// ใช้ระบบ OpenAPI แบบ Built-in ของ .NET 9 // ฟีเจอร์ใหม่ของ .NET 9 สำหรับทำเอกสาร API
builder.Services.AddOpenApi(options =>
{
    // --- ส่วนที่ 1: จัดการหน้าตาเอกสารและความปลอดภัย (Document Level) ---
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // ตั้งค่า Server URL ให้ชี้มาที่ Gateway (localhost:5177)
        // เพื่อให้ Browser ของเรายิงถูกที่
        document.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = "http://localhost:5177" }
        };

        document.Info.Title = "Concert Ticket Booking API";
        document.Info.Version = "v1";
        document.Info.Description = "ระบบจองตั๋วคอนเสิร์ต Microservices (C# / Node.js / Redis / RabbitMQ)";

        // ตั้งค่าระบบความปลอดภัย JWT
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            In = ParameterLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "กรุณาใส่เฉพาะรหัส Token (ไม่ต้องมีคำว่า Bearer)"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("Bearer", scheme);

        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
        });

        // 🔥 เพิ่มส่วนนี้: ใส่ตัวอย่าง GUID ให้กับ Path Parameter ชื่อ 'id'
        // วนลูปเพื่อใส่คำอธิบาย Parameter {id} ให้ชัดเจนตามแต่ละเส้นทาง
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var idParam = operation.Value.Parameters?.FirstOrDefault(p => p.Name == "id");
                if (idParam != null)
                {
                    if (path.Key.Contains("booking/"))
                        idParam.Description = "รหัสการจอง (Booking ID) ที่ได้รับหลังจากกดจองสำเร็จ";
                    else if (path.Key.Contains("confirm-payment/"))
                        idParam.Description = "รหัสตั๋วที่ต้องการชำระเงิน";
                    else if (path.Key.Contains("cancel/"))
                        idParam.Description = "รหัสตั๋วที่ต้องการยกเลิกเพื่อคืนที่นั่ง";
                    else
                        idParam.Description = "รหัสอ้างอิง (ID)";

                    idParam.Example = new Microsoft.OpenApi.Any.OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                }
            }
        }

        return Task.CompletedTask;
    });

    // --- ส่วนที่ 2: จัดการตัวอย่างข้อมูลใน JSON Body (Schema Level) ---
    // ส่วนนี้จะช่วยให้ Scalar มีข้อมูลกรอกให้อัตโนมัติ
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        // 1. ตัวอย่างสำหรับ Login / Register
        if (context.JsonTypeInfo.Type == typeof(UserDto))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["username"] = new Microsoft.OpenApi.Any.OpenApiString("tony"),
                ["password"] = new Microsoft.OpenApi.Any.OpenApiString("123")
            };
        }

        // 2. ตัวอย่างสำหรับการจองตั๋ว
        if (context.JsonTypeInfo.Type == typeof(BookTicketCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["concertId"] = new Microsoft.OpenApi.Any.OpenApiString("22222222-2222-2222-2222-222222222222"),
                ["seatNumber"] = new Microsoft.OpenApi.Any.OpenApiString("A1")
            };
        }

        // 3. ตัวอย่างสำหรับ Admin สร้างคอนเสิร์ตใหม่
        if (context.JsonTypeInfo.Type == typeof(CreateConcertCommand))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["name"] = new Microsoft.OpenApi.Any.OpenApiString("LISA World Tour 2026"),
                ["date"] = new Microsoft.OpenApi.Any.OpenApiString(DateTime.Now.AddMonths(6).ToString("yyyy-MM-ddTHH:mm:ssZ")),
                ["vipPrice"] = new Microsoft.OpenApi.Any.OpenApiDouble(7500),
                ["gaPrice"] = new Microsoft.OpenApi.Any.OpenApiDouble(3500),
                ["vipCapacity"] = new Microsoft.OpenApi.Any.OpenApiInteger(40),
                ["gaCapacity"] = new Microsoft.OpenApi.Any.OpenApiInteger(60)
                // หมายเหตุ: PosterFile เป็นไฟล์ ไม่ต้องใส่ Example ใน JSON
            };
        }

        return Task.CompletedTask;
    });
});

// Database & Context
// ตั้งค่า Database (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- AddDbContext ---
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Redis, MediatR, RabbitMQ
// --- ตั้งค่า Redis Connection ---
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("redis_cache:6379"));

// --- ลงทะเบียน Service ยามเฝ้าที่นั่ง ---
builder.Services.AddScoped<ITicketLockService, RedisTicketLockService>();

// --- ลงทะเบียน MediatR (CQRS) ---
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(BookTicketCommand).Assembly));

// --- ลงทะเบียน RabbitMQ Publisher ---
builder.Services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

// gRPC & HealthChecks
// --- ลงทะเบียน gRPC Service ---
builder.Services.AddGrpc();

// --- เพิ่มระบบ Health Checks ---
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis("redis_cache:6379")
    // ใช้รูปแบบ New Uri เพื่อให้ Library แยกแยะประเภทข้อมูลได้ชัดเจน
    // ใช้ async lambda เพื่อรองรับ RabbitMQ.Client เวอร์ชันใหม่ (v7+)
    .AddRabbitMQ(async sp =>
    {
        var factory = new RabbitMQ.Client.ConnectionFactory()
        {
            Uri = new Uri("amqp://guest:guest@rabbitmq_broker:5672")
        };
        // ใช้ CreateConnectionAsync และเติม await ข้างหน้า
        return await factory.CreateConnectionAsync();
    }, name: "rabbitmq");

// --- JWT Authentication Setup ---
// --- ตั้งค่าระบบ JWT ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // ปิดไว้เพราะเราคุยกันภายใน (HTTP)
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        // ใช้ Encoding.UTF8 แทน ASCII เพื่อความปลอดภัยของอักขระพิเศษ
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!)),
        ValidateIssuer = false,   // ปิดเช็คผู้ออก (ชั่วคราว)
        ValidateAudience = false, // ปิดเช็คผู้รับ (ชั่วคราว)
        // 🔥 จุดสำคัญ: ปิดการตรวจสอบวันหมดอายุ เพื่อให้ใช้ Token จาก jwt.io ได้
        ValidateLifetime = false,
        // บรรทัดนี้สำคัญมาก: ปิดการเช็คเวลาที่เหลื่อมล้ำกันระหว่างเครื่อง
        ClockSkew = TimeSpan.Zero
    };
    // 🔥 เพิ่มส่วนนี้เข้าไป
    x.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse(); // ปิดระบบพ่น Error เดิม
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "คุณไม่มีสิทธิ์เข้าถึง กรุณาเข้าสู่ระบบก่อน",
                data = default(object) // 🔥 ใช้ default(object) เพื่อแก้ Warning
            });
        },
        OnForbidden = async context =>
        { // 🔥 เพิ่มเคส 403 (มีบัตรแต่เข้าโซนนี้ไม่ได้)
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "คุณไม่มีสิทธิ์ดำเนินการในส่วนนี้",
                data = default(object)
            });
        }
    };
});
builder.Services.AddAuthorization(); // เปิดใช้ระบบกำหนดสิทธิ์

builder.Services.AddHostedService<TicketCleanupService>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("TicketBooking.Api")
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Service-A"))
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation() // ดูว่า SQL รันนานแค่ไหน
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://jaeger:4317")));

// บอกให้ MediatR ใช้ด่านตรวจ ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TicketBooking.Application.Common.Behaviors.ValidationBehavior<,>));

// บอกให้ระบบกวาดหา Validator ทั้งหมดในชั้น Application มาลงทะเบียน
builder.Services.AddValidatorsFromAssembly(typeof(TicketBooking.Application.Commands.BookTicketCommandValidator).Assembly);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // อนุญาตหน้าบ้าน
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 2. ในส่วน Middleware (ต้องวางก่อน UseAuthentication และ MapControllers)
app.UseCors("AllowFrontend"); // 🔥 วางตรงนี้!

// Correlation ID (บนสุดเพื่อให้ทุกอย่างมี ID)
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        await next();
    }
});

// --- Middleware Pipeline ---
// Global Error Handling
app.UseMiddleware<ExceptionMiddleware>(); // ดัก Error ทั้งระบบ

// Static Files & Routing
app.UseStaticFiles();
// ตั้งค่า Pipeline (Middleware)
if (app.Environment.IsDevelopment())
{
    // สร้างไฟล์เอกสาร OpenAPI (ปกติจะเข้าถึงได้ที่ /openapi/v1.json)
    app.MapOpenApi();

    // เปิดใช้งานหน้า UI ของ Scalar
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Ticket Booking API"); // ตั้งชื่อ API
        options.WithTheme(ScalarTheme.BluePlanet); // เลือกธีมสีสวยๆ (เช่น BluePlanet, Kepler, None, Purple, Solarized)
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient); // สร้างโค้ดตัวอย่างเป็น C#
    });
}

// app.UseHttpsRedirection(); // ปิดไว้ถูกแล้วครับ
// app.UseAuthorization();

// Security (ลำดับห้ามสลับ!)
app.UseAuthentication(); // เช็คว่าเป็นใคร
app.UseAuthorization();  // เช็คว่าทำอะไรได้บ้าง

// Endpoints
app.MapGrpcService<ConcertGrpcService>();

// ผูก Request ให้วิ่งไปหา Controller
app.MapControllers();

app.MapHealthChecks("/health");

// ... Migration Code ...
// Auto-Migration & Seeding
// 🔥 แก้ไขส่วนนี้ให้มีความทนทาน (Resilience) 🔥
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    // พยายามเชื่อมต่อ 5 ครั้ง ถ้าไม่ได้ให้รอครั้งละ 5 วินาที
    for (int i = 0; i < 5; i++)
    {
        try
        {
            logger.LogInformation("⏳ กำลังตรวจสอบฐานข้อมูล (รอบที่ {Step})...", i + 1);
            dbContext.Database.Migrate();
            TicketBooking.Infrastructure.Seeders.DatabaseSeeder.SeedAsync(dbContext).Wait();
            logger.LogInformation("✅ ฐานข้อมูลพร้อมใช้งานและ Seed ข้อมูลสำเร็จ!");
            break; // ถ้าสำเร็จให้หลุดออกจาก Loop
        }
        catch (Exception ex)
        {
            if (i == 4) // ถ้าครบ 5 ครั้งแล้วยังไม่ได้ ค่อยพ่น Error จริงๆ ออกมา
            {
                logger.LogCritical(ex, "❌ ไม่สามารถเชื่อมต่อฐานข้อมูลได้หลังจากพยายาม 5 ครั้ง");
                throw;
            }
            logger.LogWarning("⚠️ ฐานข้อมูลยังไม่พร้อม... กำลังรอ 5 วินาที (Error: {Msg})", ex.Message);
            Thread.Sleep(5000); // รอ 5 วินาทีก่อนลองใหม่
        }
    }
}

app.Run();