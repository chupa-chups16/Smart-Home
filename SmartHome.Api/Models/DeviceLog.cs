using SmartHome.Api.Models;

public class DeviceLog
{
    public int Id { get; set; }
    public string Action { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;
}
