using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHome.Api.Models
{
    public class Home
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Foreign key tới User
        public int UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
