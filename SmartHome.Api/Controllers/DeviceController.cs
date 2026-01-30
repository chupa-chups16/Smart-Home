using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public DeviceController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("Danh sách device");
    }

    [HttpPut("status")]
    public IActionResult UpdateStatus(UpdateDeviceStatusDto dto)
    {
        return Ok("Cập nhật trạng thái device");
    }
}
