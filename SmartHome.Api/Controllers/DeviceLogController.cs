using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/device-logs")]
public class DeviceLogController : ControllerBase
{
    private  readonly SmartHomeDbContext _db;

    public DeviceLogController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("Log thiết bị");
    }
}
