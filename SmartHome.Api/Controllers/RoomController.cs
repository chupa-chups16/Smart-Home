using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.Data;
using SmartHome.Api.Models;
using SmartHome.Api.DTOs;

namespace SmartHome.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public RoomController(SmartHomeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.Rooms.ToList());
    }

    [HttpPost]
    public IActionResult Create(CreateRoomDto dto)
    {
        var room = new Room
        {
            Name = dto.Name,
            HomeId = dto.HomeId
        };

        _db.Rooms.Add(room);
        _db.SaveChanges();

        return Ok(room);
    }
}
