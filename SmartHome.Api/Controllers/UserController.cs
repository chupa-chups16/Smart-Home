using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public UserController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Không tìm thấy user");

        return Ok(new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Không tìm thấy user");

        if (!string.IsNullOrWhiteSpace(dto.Email)
            && await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Email đã tồn tại");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            user.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(dto.Role))
            user.Role = dto.Role.Trim();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueEmailViolation(ex))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflict", detail: "Email already exists");
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Không tìm thấy user");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsUniqueEmailViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}

