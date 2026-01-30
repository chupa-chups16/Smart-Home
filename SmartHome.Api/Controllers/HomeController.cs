using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.DTOs;
using SmartHome.Api.Data;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/homes")]
public class HomeController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateHome(CreateHomeDto dto)
    {
        return Ok();
    }

    [HttpGet]
    public IActionResult GetHomes()
    {
        return Ok();
    }
}
