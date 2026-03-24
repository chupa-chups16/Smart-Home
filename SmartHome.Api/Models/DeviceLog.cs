using SmartHome.Api.Models;
namespace SmartHome.Api.Models;

public class DeviceLog
{
    public int Id { get; set; }

    public Device Device { get; set; } = null!;
}
