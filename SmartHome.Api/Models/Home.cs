using System.Collections.Generic;

namespace SmartHome.Api.Models
{
    public class Home
    {
        public int HomeId { get; set; }
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public User? User { get; set; }
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
