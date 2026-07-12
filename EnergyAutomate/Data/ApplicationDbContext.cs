using EnergyAutomate.Data.Entities;
using EnergyAutomate.Definitions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnergyAutomate.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    #region Fields

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions();

    #endregion Fields

    #region Public Constructors

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    #endregion Public Constructors

    #region Properties

    public DbSet<DeviceMinInfoData> GrowattDeviceMinInfoData { get; set; }
    public DbSet<DeviceMinLastData> GrowattDeviceMinLastData { get; set; }
    public DbSet<DeviceNoahInfoData> GrowattDeviceNoahInfoData { get; set; }
    public DbSet<DeviceNoahLastData> GrowattDeviceNoahLastData { get; set; }
    public DbSet<DeviceList> GrowattDevices { get; set; }
    public DbSet<ApiRuntimeSettings> ApiRuntimeSettings { get; set; }
    public DbSet<TibberPrice> TibberPrices { get; set; }
    public DbSet<TibberRealTimeMeasurement> TibberRealTimeMeasurements { get; set; }
    public DbSet<CodeTemplate> CodeTemplates { get; set; }
    public DbSet<CodeTemplateHistory> CodeTemplateHistories { get; set; }

    #endregion Properties

    #region Protected Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceList>().HasKey(x => x.DeviceSn);
        modelBuilder.Entity<DeviceMinInfoData>().HasKey(x => x.serialNum);
        modelBuilder.Entity<DeviceNoahInfoData>().HasKey(x => x.DeviceSn);
        modelBuilder.Entity<DeviceNoahInfoData>().Ignore(x => x.TimeSegments);
        modelBuilder.Entity<DeviceMinLastData>().HasKey(x => new { x.SerialNum, x.Time });
        modelBuilder.Entity<DeviceNoahLastData>().HasKey(x => new { x.deviceSn, x.time });

        modelBuilder.Entity<TibberRealTimeMeasurement>().HasKey(x => new { x.TS });
        modelBuilder.Entity<TibberRealTimeMeasurement>().Ignore(x => x.TotalPower);
        modelBuilder.Entity<TibberPrice>().HasKey(x => new { x.Id });

        modelBuilder.Entity<ApiRuntimeSettings>().HasKey(x => x.Id);

        // Configure CodeTemplate
        modelBuilder.Entity<CodeTemplate>()
            .HasKey(x => x.Id);
        modelBuilder.Entity<CodeTemplate>()
            .HasIndex(x => x.Key)
            .IsUnique();
        modelBuilder.Entity<CodeTemplate>()
            .HasMany(x => x.History)
            .WithOne(x => x.CodeTemplate)
            .HasForeignKey(x => x.CodeTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure CodeTemplateHistory
        modelBuilder.Entity<CodeTemplateHistory>()
            .HasKey(x => x.Id);
        modelBuilder.Entity<CodeTemplateHistory>()
            .HasIndex(x => new { x.CodeTemplateId, x.Version })
            .IsUnique();

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

    #endregion Protected Methods
}
