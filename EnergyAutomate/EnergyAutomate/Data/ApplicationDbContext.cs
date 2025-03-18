using Growatt.OSS;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Tibber.Sdk;

namespace EnergyAutomate.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices { get; set; }

    public DbSet<DeviceNoahInfo> DeviceNoahInfo { get; set; }

    public DbSet<DeviceNoahLastData> DeviceNoahLastData { get; set; }

    public DbSet<RealTimeMeasurementExtention> RealTimeMeasurements { get; set; }

    public DbSet<ApiPrice> Prices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Device>().HasKey(x => x.DeviceSn);
        modelBuilder.Entity<DeviceNoahInfo>().HasKey(x => x.DeviceSn);
        modelBuilder.Entity<DeviceNoahInfo>().Ignore(x => x.TimeSegments);
        modelBuilder.Entity<DeviceNoahLastData>().HasKey(x => new { x.deviceSn, x.time });

        modelBuilder.Entity<RealTimeMeasurementExtention>().HasKey(x => new { x.Timestamp });
        modelBuilder.Entity<ApiPrice>().HasKey(x => new { x.StartsAt });

        // Set all string properties to be nullable
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) && !property.IsPrimaryKey())
                {
                    property.IsNullable = true;
                }
            }
        }

        // other configurations
    }
}
