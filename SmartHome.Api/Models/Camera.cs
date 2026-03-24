using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHome.Api.Models;

[Table("Camera")]
public class Camera
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DeviceId { get; set; }

    public Device Device { get; set; } = null!;
}