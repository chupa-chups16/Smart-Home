using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.Models;

namespace SmartHome.Api.Services;

public class CameraService : ICameraService
{
    private readonly SmartHomeDbContext _db;

    public CameraService(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<Camera>> GetAllAsync()
    {
        return await _db.Cameras.ToListAsync();
    }

    public async Task<Camera?> GetByIdAsync(int id)
    {
        return await _db.Cameras.FindAsync(id);
    }
}