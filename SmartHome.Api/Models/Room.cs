namespace SmartHome.Api.Models;
public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public int HomeId { get; set; }
    public Home Home { get; set; } = null!;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
