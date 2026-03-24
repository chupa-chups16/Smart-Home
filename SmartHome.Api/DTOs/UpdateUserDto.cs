using System.ComponentModel.DataAnnotations;

namespace SmartHome.Api.DTOs;

public class UpdateUserDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }
}
