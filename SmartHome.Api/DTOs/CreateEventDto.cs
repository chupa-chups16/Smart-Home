using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class CreateEventDto
{
    [Required]
    [MaxLength(200)] 
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
