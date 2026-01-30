using SmartHome.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    public ICollection<Home> Homes { get; set; } = new List<Home>();
}
