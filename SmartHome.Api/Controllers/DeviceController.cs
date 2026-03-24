using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;
using SmartHome.Api.Models;
using System.Security.Claims;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Roles = "User,Admin")]
public class DeviceController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public DeviceController(SmartHomeDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    private bool IsPrivilegedRole() => User.IsInRole("Admin");

    private IQueryable<Device> ApplyOwnershipFilter(IQueryable<Device> query, int userId)
    {
        return from d in query
               join r in _db.Rooms.AsNoTracking() on d.RoomId equals r.RoomId
               join h in _db.Homes.AsNoTracking() on r.HomeId equals h.HomeId
               where h.UserId == userId
               select d;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        IQueryable<Device> query = _db.Devices.AsNoTracking();

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = ApplyOwnershipFilter(query, userId);
        }

        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        IQueryable<Device> query = _db.Devices.AsNoTracking().Where(d => d.DeviceId == id);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = ApplyOwnershipFilter(query, userId);
        }

        var device = await query.FirstOrDefaultAsync();
        if (device == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Device not found");

        return Ok(device);
    }

    [HttpGet("by-room/{roomId}")]
    public async Task<IActionResult> GetByRoom(int roomId)
    {
        IQueryable<Device> query = _db.Devices.AsNoTracking().Where(d => d.RoomId == roomId);

        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            var roomOwned = await _db.Rooms
                .AsNoTracking()
                .Join(_db.Homes.AsNoTracking(),
                    r => r.HomeId,
                    h => h.HomeId,
                    (r, h) => new { r.RoomId, h.UserId })
                .AnyAsync(x => x.RoomId == roomId && x.UserId == userId);

            if (!roomOwned)
                return Ok(new List<Device>());

            query = ApplyOwnershipFilter(query, userId);
        }

        return Ok(await query.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var name = dto.Name.Trim();
        if (name.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Name is required");

        var type = dto.Type.Trim();
        if (type.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Type is required");

        IQueryable<Room> roomQuery = _db.Rooms.AsNoTracking().Where(r => r.RoomId == dto.RoomId);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            roomQuery = from r in roomQuery
                        join h in _db.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                        where h.UserId == userId
                        select r;
        }

        var roomExists = await roomQuery.AnyAsync();
        if (!roomExists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Room does not exist");

        var device = new Device
        {
            RoomId = dto.RoomId,
            Name = name,
            Type = type,
            Status = dto.Status
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = device.DeviceId }, device);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeviceStatusDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        IQueryable<Device> query = _db.Devices.Where(d => d.DeviceId == id);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = ApplyOwnershipFilter(query, userId);
        }

        var device = await query.FirstOrDefaultAsync();
        if (device == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Device not found");

        device.Status = dto.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        IQueryable<Device> query = _db.Devices.Where(d => d.DeviceId == id);
        if (!IsPrivilegedRole())
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            query = ApplyOwnershipFilter(query, userId);
        }

        var device = await query.FirstOrDefaultAsync();
        if (device == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Device not found");

        _db.Devices.Remove(device);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
