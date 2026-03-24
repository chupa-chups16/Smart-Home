using System.ComponentModel.DataAnnotations;

namespace SmartHome.Contracts.DTOs;

public class CreateFireAlertDto
{
    [Required]
    public int DeviceId { get; set; }

    [Required]
    public double Temperature { get; set; }

    public double? Rate { get; set; }

    public DateTime? DetectedAtUtc { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = "alert-camera-service";

    [MaxLength(500)]
    public string? CameraFilePath { get; set; }
}
