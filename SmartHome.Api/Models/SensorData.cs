namespace SmartHome.Api.Models;
public class SensorData
{
    public int ID { get; set; }
    public string SensorType { get; set; } = null!;
    public double Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DeviceID { get; set; }
    public Device Device { get; set; } = null!;
}
