using System.ComponentModel.DataAnnotations;

namespace SmartHome.Contracts.DTOs;

public class CreateSensorDataDto
{
    [Required]
    public int DeviceId { get; set; }

    [Required]
    public double Value { get; set; }
}
