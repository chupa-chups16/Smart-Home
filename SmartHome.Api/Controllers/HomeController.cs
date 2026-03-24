using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;
using SmartHome.Api.Models;
using System.Security.Claims;

namespace SmartHome.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User,Admin")]
public class HomeController : ControllerBase
{
    private readonly SmartHomeDbContext _context;

    public HomeController(SmartHomeDbContext context)
    {
        _context = context;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    private bool IsPrivilegedRole() => User.IsInRole("Admin");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHomeDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

        var name = dto.Name.Trim();
        if (name.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Name is required");

        var home = new Home
        {
            UserId = userId,
            Name = name
        };

        _context.Homes.Add(home);
        await _context.SaveChangesAsync();

        var result = new HomeDto
        {
            HomeId = home.HomeId,
            UserId = home.UserId,
            Name = home.Name
        };

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        IQueryable<Home> query = _context.Homes.AsNoTracking();

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = query.Where(h => h.UserId == userId);
        }

        var homes = await query
            .Select(h => new HomeDto
            {
                HomeId = h.HomeId,
                UserId = h.UserId,
                Name = h.Name
            })
            .ToListAsync();

        return Ok(homes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        IQueryable<Home> query = _context.Homes.AsNoTracking().Where(h => h.HomeId == id);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = query.Where(h => h.UserId == userId);
        }

        var home = await query
            .Select(h => new HomeDto
            {
                HomeId = h.HomeId,
                UserId = h.UserId,
                Name = h.Name
            })
            .FirstOrDefaultAsync();

        if (home == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Home not found");

        return Ok(home);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateHomeDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var name = dto.Name.Trim();
        if (name.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Name is required");

        IQueryable<Home> query = _context.Homes.Where(h => h.HomeId == id);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = query.Where(h => h.UserId == userId);
        }

        var home = await query.FirstOrDefaultAsync();
        if (home == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Home not found");

        home.Name = name;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        IQueryable<Home> query = _context.Homes.Where(h => h.HomeId == id);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = query.Where(h => h.UserId == userId);
        }

        var home = await query.FirstOrDefaultAsync();
        if (home == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Home not found");

        _context.Homes.Remove(home);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
