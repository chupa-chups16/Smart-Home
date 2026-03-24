using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;
using SmartHome.Api.Models;
using SmartHome.Api.Services;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SmartHomeDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(SmartHomeDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var name = dto.Name.Trim();
        if (name.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Name is required");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (email.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Email is required");

        var existingUser = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existingUser != null)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Email already exists");

        var user = new User
        {
            Name = name,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueEmailViolation(ex))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Email already exists");
        }

        return Ok("Register successful");
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (email.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Email is required");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid email or password");

        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
        if (!isValid)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid email or password");

        var token = _tokenService.CreateToken(user);

        return Ok(new
        {
            token,
            name = user.Name
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logout successful" });
    }

    private static bool IsUniqueEmailViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
