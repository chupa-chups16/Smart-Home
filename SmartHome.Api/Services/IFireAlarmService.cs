namespace SmartHome.Api.Services;

public interface IFireAlarmService
{
    Task<bool> CheckFireAsync(double sensorValue);
}