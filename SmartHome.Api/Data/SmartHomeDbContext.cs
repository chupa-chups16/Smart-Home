namespace SmartHome.Api.Data;

using Microsoft.EntityFrameworkCore;
using SmartHome.Api.Models;

public class SmartHomeDbContext : DbContext
{
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<SensorData> SensorDatas => Set<SensorData>();
    public DbSet<DeviceLog> DeviceLogs => Set<DeviceLog>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
}
