using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHome.Api.Models;

[Table("Event")]
public class Event
{
    [Key]
    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    // Foreign key
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}