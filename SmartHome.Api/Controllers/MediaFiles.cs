using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.DTOs;
using SmartHome.Api.Models;
using System.Security.Claims;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/mediafiles")]
[Authorize]
public class MediaFilesController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public MediaFilesController(SmartHomeDbContext db)
    {
        _db = db;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    [HttpGet]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetAll()
    {
        IQueryable<MediaFile> query = _db.MediaFiles.AsNoTracking();

        if (!User.IsInRole("Admin"))
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            var ownedDeviceIds = from d in _db.Devices.AsNoTracking()
                                 join r in _db.Rooms.AsNoTracking() on d.RoomId equals r.RoomId
                                 join h in _db.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                                 where h.UserId == userId
                                 select d.DeviceId;

            query = query.Where(m => m.CreatedByUserId == userId ||
                                     (m.DeviceId.HasValue && ownedDeviceIds.Contains(m.DeviceId.Value)));
        }

        return Ok(await query.OrderByDescending(m => m.CreatedAt).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        IQueryable<MediaFile> query = _db.MediaFiles.AsNoTracking().Where(m => m.Id == id);

        if (!User.IsInRole("Admin"))
        {
            if (!TryGetUserId(out var userId))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: "Invalid user context");

            var ownedDeviceIds = from d in _db.Devices.AsNoTracking()
                                 join r in _db.Rooms.AsNoTracking() on d.RoomId equals r.RoomId
                                 join h in _db.Homes.AsNoTracking() on r.HomeId equals h.HomeId
                                 where h.UserId == userId
                                 select d.DeviceId;

            query = query.Where(m => m.CreatedByUserId == userId ||
                                     (m.DeviceId.HasValue && ownedDeviceIds.Contains(m.DeviceId.Value)));
        }

        var media = await query.FirstOrDefaultAsync();
        if (media == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Media file not found");

        return Ok(media);
    }

    [HttpPost]
    [Authorize(Roles = "Service,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMediaFileDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var fileName = dto.FileName.Trim();
        if (fileName.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "FileName is required");

        var filePath = dto.FilePath.Trim();
        if (filePath.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "FilePath is required");

        var fileType = dto.FileType.Trim();
        if (fileType.Length == 0)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "FileType is required");

        if (dto.DeviceId.HasValue)
        {
            var deviceExists = await _db.Devices.AnyAsync(d => d.DeviceId == dto.DeviceId.Value);
            if (!deviceExists)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad Request", detail: "Device does not exist");
        }

        int? createdByUserId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var parsedUserId))
            createdByUserId = parsedUserId;

        var media = new MediaFile
        {
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType,
            DeviceId = dto.DeviceId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.MediaFiles.Add(media);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
    }
}
