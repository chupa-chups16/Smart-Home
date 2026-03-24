namespace SmartHome.Api.DTOs;

public class RoomDto
{
    public int RoomId { get; set; }
    public int HomeId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Name => RoomName;
    public string? Description { get; set; }
}
