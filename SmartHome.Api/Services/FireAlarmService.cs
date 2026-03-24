namespace SmartHome.Api.Services;

public class FireAlarmService : IFireAlarmService
{
    private const double FIRE_THRESHOLD = 70; // ví dụ ngưỡng khói

    public Task<bool> CheckFireAsync(double sensorValue)
    {
        bool isFire = sensorValue >= FIRE_THRESHOLD;
        return Task.FromResult(isFire);
    }
}