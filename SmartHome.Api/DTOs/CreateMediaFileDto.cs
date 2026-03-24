using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class CreateMediaFileDto
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string FileType { get; set; } = string.Empty;

    public int? DeviceId { get; set; }
}
