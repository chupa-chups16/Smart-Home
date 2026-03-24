using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class UpdateDeviceStatusDto
{
    [Required]
    public bool Status { get; set; }
}
