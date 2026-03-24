namespace SmartHome.Api.DTOs;

public class HomeDto
{
    public int HomeId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}
