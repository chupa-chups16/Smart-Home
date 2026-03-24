using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;

namespace SmartHome.Api.Data
{
    public class SmartHomeDbContext : DbContext
    {
        public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options)
            : base(options)
        {
        }

        // ===== Core SmartHome Structure =====
        public DbSet<User> Users { get; set; }
        public DbSet<Home> Homes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Device> Devices { get; set; }

        // ===== Other Features =====
        public DbSet<SensorData> SensorData { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Camera> Cameras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Home - Room (1 - N)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Home)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HomeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room - Device (1 - N)
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Room)
                .WithMany(r => r.Devices)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // User - Home (1 - N)
            modelBuilder.Entity<Home>()
                .HasOne(h => h.User)
                .WithMany(u => u.Homes)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
