using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/sensors")]
public class SensorController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public SensorController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public IActionResult Create(CreateSensorDataDto dto)
    {
        return Ok("Nhận dữ liệu sensor");
    }
}
