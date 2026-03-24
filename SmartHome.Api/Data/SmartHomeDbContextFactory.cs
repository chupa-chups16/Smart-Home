using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SmartHome.Api.Data
{
    public class SmartHomeDbContextFactory : IDesignTimeDbContextFactory<SmartHomeDbContext>
    {
        public SmartHomeDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<SmartHomeDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

            var optionsBuilder = new DbContextOptionsBuilder<SmartHomeDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SmartHomeDbContext(optionsBuilder.Options);
        }
    }
}
