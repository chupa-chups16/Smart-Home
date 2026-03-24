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
public class RoomController : ControllerBase
{
    private readonly SmartHomeDbContext _context;

    public RoomController(SmartHomeDbContext context)
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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? homeId)
    {
        IQueryable<Room> query = _context.Rooms.AsNoTracking();

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = from r in query
                    join h in _context.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                    where h.UserId == userId
                    select r;
        }

        if (homeId.HasValue)
            query = query.Where(r => r.HomeId == homeId.Value);

        var rooms = await query
            .Select(r => new RoomDto
            {
                RoomId = r.RoomId,
                HomeId = r.HomeId,
                RoomName = r.RoomName,
                Description = r.Description
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        IQueryable<Room> query = _context.Rooms.AsNoTracking().Where(r => r.RoomId == id);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = from r in query
                    join h in _context.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                    where h.UserId == userId
                    select r;
        }

        var room = await query
            .Select(r => new RoomDto
            {
                RoomId = r.RoomId,
                HomeId = r.HomeId,
                RoomName = r.RoomName,
                Description = r.Description
            })
            .FirstOrDefaultAsync();

        if (room == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Room not found");

        return Ok(room);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var roomName = dto.RoomName.Trim();
        if (roomName.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "RoomName is required");

        IQueryable<Home> homeQuery = _context.Homes.AsNoTracking().Where(h => h.HomeId == dto.HomeId);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            homeQuery = homeQuery.Where(h => h.UserId == userId);
        }

        var homeExists = await homeQuery.AnyAsync();
        if (!homeExists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Home does not exist");

        var room = new Room
        {
            HomeId = dto.HomeId,
            RoomName = roomName,
            Description = dto.Description?.Trim()
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        var result = new RoomDto
        {
            RoomId = room.RoomId,
            HomeId = room.HomeId,
            RoomName = room.RoomName,
            Description = room.Description
        };

        return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var roomName = dto.RoomName.Trim();
        if (roomName.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "RoomName is required");

        IQueryable<Room> query = _context.Rooms.Where(r => r.RoomId == id);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = from r in query
                    join h in _context.Homes on r.HomeId equals h.HomeId
                    where h.UserId == userId
                    select r;
        }

        var room = await query.FirstOrDefaultAsync();
        if (room == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Room not found");

        room.RoomName = roomName;
        room.Description = dto.Description?.Trim();
        await _context.SaveChangesAsync();

        var result = new RoomDto
        {
            RoomId = room.RoomId,
            HomeId = room.HomeId,
            RoomName = room.RoomName,
            Description = room.Description
        };

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        IQueryable<Room> query = _context.Rooms.Where(r => r.RoomId == id);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = from r in query
                    join h in _context.Homes on r.HomeId equals h.HomeId
                    where h.UserId == userId
                    select r;
        }

        var room = await query.FirstOrDefaultAsync();
        if (room == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Room not found");

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
