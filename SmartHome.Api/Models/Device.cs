namespace SmartHome.Api.Models
{
    public class Device
    {
        public int DeviceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool Status { get; set; }

        public int RoomId { get; set; }

        public Room Room { get; set; } = null!;
    }
}
