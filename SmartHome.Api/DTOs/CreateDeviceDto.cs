using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class CreateDeviceDto
{
    [Required]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public bool Status { get; set; }
}
