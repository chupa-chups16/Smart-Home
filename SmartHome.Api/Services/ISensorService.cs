using SmartHome.Api.Models;

namespace SmartHome.Api.Services;

public interface ISensorService
{
    Task<List<SensorData>> GetByDeviceAsync(int deviceId);
    Task<SensorData> CreateAsync(SensorData data);
}