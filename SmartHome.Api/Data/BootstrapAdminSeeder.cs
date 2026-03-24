using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;

namespace SmartHome.Api.Data;

public static class BootstrapAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var section = config.GetSection("BootstrapAdmin");
        var emailRaw = section["Email"];
        var password = section["Password"];
        var name = section["Name"];

        if (string.IsNullOrWhiteSpace(emailRaw) || string.IsNullOrWhiteSpace(password))
            return;

        var email = emailRaw.Trim().ToLowerInvariant();
        var adminName = string.IsNullOrWhiteSpace(name) ? "Admin" : name.Trim();

        var db = services.GetRequiredService<SmartHomeDbContext>();

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
        {
            existing.Name = adminName;

            if (!string.Equals(existing.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                existing.Role = "Admin";
            }

            if (!BCrypt.Net.BCrypt.Verify(password, existing.Password))
            {
                existing.Password = BCrypt.Net.BCrypt.HashPassword(password);
            }

            await db.SaveChangesAsync();

            return;
        }

        var admin = new User
        {
            Name = adminName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
