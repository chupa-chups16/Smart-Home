using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Data;
using SmartHome.Api.Models;

namespace SmartHome.Api.Services;

public class SensorService : ISensorService
{
    private readonly SmartHomeDbContext _db;

    public SensorService(SmartHomeDbContext db)
    {
        _db = db;
    }

    public async Task<List<SensorData>> GetByDeviceAsync(int deviceId)
    {
        return await _db.SensorData
            .AsNoTracking() // tối ưu đọc
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<SensorData> CreateAsync(SensorData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        data.CreatedAt = DateTime.UtcNow;

        await _db.SensorData.AddAsync(data);
        await _db.SaveChangesAsync();

        return data;
    }
}