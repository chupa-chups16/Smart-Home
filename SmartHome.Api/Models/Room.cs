using System.Collections.Generic;

namespace SmartHome.Api.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int HomeId { get; set; }

        public Home? Home { get; set; }

        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
