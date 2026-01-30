using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;   
using SmartHome.Api.Models;  
using SmartHome.Api.DTOs;    

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public AuthController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        return Ok("Login OK (chưa xử lý)");
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterDto dto)
    {
        return Ok("Register OK (chưa xử lý)");
    }
}
