using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class CreateRoomDto
{
    [Required]
    public int HomeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoomName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}
