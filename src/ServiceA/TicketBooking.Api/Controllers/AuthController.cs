using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketBooking.Application.Interfaces;
using TicketBooking.Domain.Entities;
using TicketBooking.Application.Common.Responses;

namespace TicketBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(IApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDto request)
    {
        var user = new User { Username = request.Username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(default);

        // แทนที่จะส่งแค่ ID เปล่าๆ เราส่งเป็น Object ที่มีรายละเอียด
        var responseData = new { userId = user.Id, username = user.Username };
        return Ok(ApiResponse<object>.Ok(responseData, "สมัครสมาชิกสำเร็จ"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<string>.Fail("รหัสผ่านไม่ถูกต้อง"));

        var token = CreateToken(user);

        // ส่ง Token พร้อมข้อมูล User เบื้องต้น
        var responseData = new
        {
            token = token,
            user = new { id = user.Id, username = user.Username }
        };
        return Ok(ApiResponse<object>.Ok(responseData, "เข้าสู่ระบบสำเร็จ"));
    }

    private string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role) // 🔥 เพิ่ม: ฝัง Role เข้าไปใน Token
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: null,
            claims: claims,
            expires: DateTime.Now.AddDays(1), // บังคับมีวันหมดอายุ
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// ข้อมูลสำหรับการสมัครสมาชิกและเข้าสู่ระบบ
/// </summary>
/// <param name="Username">ตัวอย่าง: tony</param>
/// <param name="Password">ตัวอย่าง: 123</param>
public record UserDto(string Username, string Password);