using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Contracts.DTOs;
using SmartHome.Api.Models;
using SmartHome.Api.Services;
using System.Security.Claims;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/sensors")]
[Authorize]
public class SensorController : ControllerBase
{
    private readonly ISensorService _sensorService;
    private readonly SmartHomeDbContext _db;

    public SensorController(ISensorService sensorService, SmartHomeDbContext db)
    {
        _sensorService = sensorService;
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    [HttpGet("by-device/{deviceId}")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetByDevice(int deviceId)
    {
        if (!User.IsInRole("Admin"))
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            var ownsDevice = await _db.Devices
                .AsNoTracking()
                .Join(_db.Rooms.AsNoTracking(), d => d.RoomId, r => r.RoomId, (d, r) => new { d.DeviceId, r.HomeId })
                .Join(_db.Homes.AsNoTracking(), x => x.HomeId, h => h.HomeId, (x, h) => new { x.DeviceId, h.UserId })
                .AnyAsync(x => x.DeviceId == deviceId && x.UserId == userId);

            if (!ownsDevice)
                return Ok(new List<SensorData>());
        }

        var result = await _sensorService.GetByDeviceAsync(deviceId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Service,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSensorDataDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var deviceExists = await _db.Devices.AnyAsync(d => d.DeviceId == dto.DeviceId);
        if (!deviceExists)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Device does not exist");

        var data = new SensorData
        {
            DeviceId = dto.DeviceId,
            Value = (float)dto.Value
        };

        var result = await _sensorService.CreateAsync(data);
        return Ok(result);
    }
}
