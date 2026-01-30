namespace SmartHome.Api.DTOs;

public class CreateRoomDto
{
    public string Name { get; set; } = null!;
    public int HomeId { get; set; }
}
