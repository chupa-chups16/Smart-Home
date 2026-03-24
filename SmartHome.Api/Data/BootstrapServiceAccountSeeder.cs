using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;

namespace SmartHome.Api.Data;

public static class BootstrapServiceAccountSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var section = config.GetSection("BootstrapServiceAccount");
        var emailRaw = section["Email"];
        var password = section["Password"];
        var name = section["Name"];

        if (string.IsNullOrWhiteSpace(emailRaw) || string.IsNullOrWhiteSpace(password))
            return;

        var email = emailRaw.Trim().ToLowerInvariant();
        var serviceName = string.IsNullOrWhiteSpace(name) ? "Service Account" : name.Trim();

        var db = services.GetRequiredService<SmartHomeDbContext>();
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existing != null)
        {
            existing.Name = serviceName;

            if (!string.Equals(existing.Role, "Service", StringComparison.OrdinalIgnoreCase))
            {
                existing.Role = "Service";
            }

            if (!BCrypt.Net.BCrypt.Verify(password, existing.Password))
            {
                existing.Password = BCrypt.Net.BCrypt.HashPassword(password);
            }

            await db.SaveChangesAsync();
            return;
        }

        var serviceUser = new User
        {
            Name = serviceName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Service",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(serviceUser);
        await db.SaveChangesAsync();
    }
}
