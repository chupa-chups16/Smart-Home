using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;
using SmartHome.Contracts.DTOs;
using SmartHome.Api.Models;
using System.Security.Claims;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public EventController(SmartHomeDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out userId);
    }

    [HttpGet]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetMyEvents()
    {
        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid token claims");

        var events = await _db.Events
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid token claims");

        var title = dto.Title.Trim();
        if (title.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Title is required");

        var model = new Event
        {
            Title = title,
            Description = dto.Description?.Trim(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(model);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.EventId }, model);
    }

    [HttpPost("fire-alert")]
    [Authorize(Roles = "Service,Admin")]
    public async Task<IActionResult> CreateFireAlert([FromBody] CreateFireAlertDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var deviceContext = await (from d in _db.Devices.AsNoTracking()
                                   join r in _db.Rooms.AsNoTracking() on d.RoomId equals r.RoomId
                                   join h in _db.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                                   where d.DeviceId == dto.DeviceId
                                   select new
                                   {
                                       d.DeviceId,
                                       d.Name,
                                       r.RoomName,
                                       HomeName = h.Name,
                                       h.UserId
                                   }).FirstOrDefaultAsync();

        if (deviceContext == null)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Device does not exist or is not assigned to a home");

        var detectedAtUtc = dto.DetectedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var source = string.IsNullOrWhiteSpace(dto.Source) ? "alert-camera-service" : dto.Source.Trim();
        var ratePart = dto.Rate.HasValue ? $", rate={dto.Rate.Value:0.###} C/s" : string.Empty;
        var cameraPart = string.IsNullOrWhiteSpace(dto.CameraFilePath)
            ? string.Empty
            : $", camera={dto.CameraFilePath.Trim()}";

        var model = new Event
        {
            Title = $"Fire Alert - {deviceContext.Name}",
            Description =
                $"source={source}, deviceId={deviceContext.DeviceId}, room={deviceContext.RoomName}, " +
                $"home={deviceContext.HomeName}, temperature={dto.Temperature:0.##} C{ratePart}, " +
                $"detectedAtUtc={detectedAtUtc:O}{cameraPart}",
            UserId = deviceContext.UserId,
            CreatedAt = detectedAtUtc
        };

        _db.Events.Add(model);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.EventId }, model);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid token claims");

        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == id && e.UserId == userId);

        if (ev == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Event not found");

        return Ok(ev);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUserId(out var userId))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid token claims");

        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.EventId == id && e.UserId == userId);

        if (ev == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Event not found");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
