namespace SmartHome.Api.DTOs;

public class CreateDeviceDto
{
    public string Name { get; set; } = null!;
    public int RoomId { get; set; }
}
