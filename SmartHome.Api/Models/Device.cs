namespace SmartHome.Api.Models;

public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int IsOn { get; set; }
    public int RoomId {get; set;}
    public Room Room { get; set; } = null!;
}
