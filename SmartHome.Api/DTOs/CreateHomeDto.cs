using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class CreateHomeDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
