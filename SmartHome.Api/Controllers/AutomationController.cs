using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/automations")]
public class AutomationController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public AutomationController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("Danh sách automation");
    }
}
