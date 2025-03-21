﻿// <auto-generated />
using System;
using EnergyAutomate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EnergyAutomate.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DeviceNoahInfo", b =>
                {
                    b.Property<string>("DeviceSn")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Address")
                        .HasColumnType("int");

                    b.Property<string>("Alias")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AssociatedInvSn")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("BmsVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ChargingSocHighLimit")
                        .HasColumnType("int");

                    b.Property<int>("ChargingSocLowLimit")
                        .HasColumnType("int");

                    b.Property<double>("ComponentPower")
                        .HasColumnType("float");

                    b.Property<string>("DatalogSn")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DefaultPower")
                        .HasColumnType("int");

                    b.Property<int>("EbmOrderNum")
                        .HasColumnType("int");

                    b.Property<string>("FwVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("LastUpdateTime")
                        .HasColumnType("bigint");

                    b.Property<string>("LastUpdateTimeText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Location")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Lost")
                        .HasColumnType("bit");

                    b.Property<string>("Model")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MpptVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OtaDeviceTypeCodeHigh")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OtaDeviceTypeCodeLow")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PdVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PortName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("SmartSocketPower")
                        .HasColumnType("float");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<long>("SysTime")
                        .HasColumnType("bigint");

                    b.Property<int>("TempType")
                        .HasColumnType("int");

                    b.Property<int>("Time1Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time1End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time1Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time1Power")
                        .HasColumnType("int");

                    b.Property<string>("Time1Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time2Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time2End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time2Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time2Power")
                        .HasColumnType("int");

                    b.Property<string>("Time2Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time3Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time3End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time3Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time3Power")
                        .HasColumnType("int");

                    b.Property<string>("Time3Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time4Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time4End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time4Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time4Power")
                        .HasColumnType("int");

                    b.Property<string>("Time4Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time5Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time5End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time5Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time5Power")
                        .HasColumnType("int");

                    b.Property<string>("Time5Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time6Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time6End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time6Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time6Power")
                        .HasColumnType("int");

                    b.Property<string>("Time6Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time7Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time7End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time7Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time7Power")
                        .HasColumnType("int");

                    b.Property<string>("Time7Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time8Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time8End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time8Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time8Power")
                        .HasColumnType("int");

                    b.Property<string>("Time8Start")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time9Enable")
                        .HasColumnType("int");

                    b.Property<string>("Time9End")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Time9Mode")
                        .HasColumnType("int");

                    b.Property<int>("Time9Power")
                        .HasColumnType("int");

                    b.Property<string>("Time9Start")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("DeviceSn");

                    b.ToTable("DeviceNoahInfo");
                });

            modelBuilder.Entity("DeviceNoahLastData", b =>
                {
                    b.Property<string>("deviceSn")
                        .HasColumnType("nvarchar(450)");

                    b.Property<long>("time")
                        .HasColumnType("bigint");

                    b.Property<int>("acCoupleProtectStatus")
                        .HasColumnType("int");

                    b.Property<int>("acCoupleWarnStatus")
                        .HasColumnType("int");

                    b.Property<int>("battery1ProtectStatus")
                        .HasColumnType("int");

                    b.Property<string>("battery1SerialNum")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("battery1Soc")
                        .HasColumnType("int");

                    b.Property<float>("battery1Temp")
                        .HasColumnType("real");

                    b.Property<float>("battery1TempF")
                        .HasColumnType("real");

                    b.Property<int>("battery1WarnStatus")
                        .HasColumnType("int");

                    b.Property<int>("battery2ProtectStatus")
                        .HasColumnType("int");

                    b.Property<string>("battery2SerialNum")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("battery2Soc")
                        .HasColumnType("int");

                    b.Property<float>("battery2Temp")
                        .HasColumnType("real");

                    b.Property<float>("battery2TempF")
                        .HasColumnType("real");

                    b.Property<int>("battery2WarnStatus")
                        .HasColumnType("int");

                    b.Property<int>("battery3ProtectStatus")
                        .HasColumnType("int");

                    b.Property<string>("battery3SerialNum")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("battery3Soc")
                        .HasColumnType("int");

                    b.Property<float>("battery3Temp")
                        .HasColumnType("real");

                    b.Property<float>("battery3TempF")
                        .HasColumnType("real");

                    b.Property<int>("battery3WarnStatus")
                        .HasColumnType("int");

                    b.Property<int>("battery4ProtectStatus")
                        .HasColumnType("int");

                    b.Property<string>("battery4SerialNum")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("battery4Soc")
                        .HasColumnType("int");

                    b.Property<float>("battery4Temp")
                        .HasColumnType("real");

                    b.Property<float>("battery4TempF")
                        .HasColumnType("real");

                    b.Property<int>("battery4WarnStatus")
                        .HasColumnType("int");

                    b.Property<int>("batteryCycles")
                        .HasColumnType("int");

                    b.Property<int>("batteryPackageQuantity")
                        .HasColumnType("int");

                    b.Property<int>("batterySoh")
                        .HasColumnType("int");

                    b.Property<int>("chargeSocLimit")
                        .HasColumnType("int");

                    b.Property<int>("ctFlag")
                        .HasColumnType("int");

                    b.Property<float>("ctSelfPower")
                        .HasColumnType("real");

                    b.Property<string>("datalogSn")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("dischargeSocLimit")
                        .HasColumnType("int");

                    b.Property<float>("eacMonth")
                        .HasColumnType("real");

                    b.Property<float>("eacToday")
                        .HasColumnType("real");

                    b.Property<float>("eacTotal")
                        .HasColumnType("real");

                    b.Property<float>("eacYear")
                        .HasColumnType("real");

                    b.Property<int>("faultStatus")
                        .HasColumnType("int");

                    b.Property<int>("heatingStatus")
                        .HasColumnType("int");

                    b.Property<float>("householdLoadApartFromGroplug")
                        .HasColumnType("real");

                    b.Property<int>("isAgain")
                        .HasColumnType("int");

                    b.Property<float>("maxCellVoltage")
                        .HasColumnType("real");

                    b.Property<float>("minCellVoltage")
                        .HasColumnType("real");

                    b.Property<int>("mpptProtectStatus")
                        .HasColumnType("int");

                    b.Property<int>("onOffGrid")
                        .HasColumnType("int");

                    b.Property<float>("pac")
                        .HasColumnType("real");

                    b.Property<int>("pdWarnStatus")
                        .HasColumnType("int");

                    b.Property<float>("ppv")
                        .HasColumnType("real");

                    b.Property<float>("pv1Current")
                        .HasColumnType("real");

                    b.Property<float>("pv1Temp")
                        .HasColumnType("real");

                    b.Property<float>("pv1Voltage")
                        .HasColumnType("real");

                    b.Property<float>("pv2Current")
                        .HasColumnType("real");

                    b.Property<float>("pv2Temp")
                        .HasColumnType("real");

                    b.Property<float>("pv2Voltage")
                        .HasColumnType("real");

                    b.Property<float>("pv3Current")
                        .HasColumnType("real");

                    b.Property<float>("pv3Temp")
                        .HasColumnType("real");

                    b.Property<float>("pv3Voltage")
                        .HasColumnType("real");

                    b.Property<float>("pv4Current")
                        .HasColumnType("real");

                    b.Property<float>("pv4Temp")
                        .HasColumnType("real");

                    b.Property<float>("pv4Voltage")
                        .HasColumnType("real");

                    b.Property<int>("settableTimePeriod")
                        .HasColumnType("int");

                    b.Property<int>("status")
                        .HasColumnType("int");

                    b.Property<float>("systemTemp")
                        .HasColumnType("real");

                    b.Property<string>("timeStr")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("totalBatteryPackChargingPower")
                        .HasColumnType("int");

                    b.Property<int>("totalBatteryPackChargingStatus")
                        .HasColumnType("int");

                    b.Property<int>("totalBatteryPackSoc")
                        .HasColumnType("int");

                    b.Property<float>("totalHouseholdLoad")
                        .HasColumnType("real");

                    b.Property<int>("workMode")
                        .HasColumnType("int");

                    b.HasKey("deviceSn", "time");

                    b.ToTable("DeviceNoahLastData");
                });

            modelBuilder.Entity("EnergyAutomate.ApiPrice", b =>
                {
                    b.Property<DateTime>("StartsAt")
                        .HasColumnType("datetime2");

                    b.Property<int?>("Level")
                        .HasColumnType("int");

                    b.Property<decimal?>("Total")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("StartsAt");

                    b.ToTable("Prices");
                });

            modelBuilder.Entity("EnergyAutomate.Data.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("EnergyAutomate.RealTimeMeasurementExtention", b =>
                {
                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("AccumulatedConsumption")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("AccumulatedConsumptionLastHour")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("AccumulatedCost")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("AccumulatedProduction")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("AccumulatedProductionLastHour")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("AccumulatedReward")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("AveragePower")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("AvgPowerLoad")
                        .HasColumnType("int");

                    b.Property<string>("Currency")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("CurrentPhase1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CurrentPhase2")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("CurrentPhase3")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("DeviceInfos")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("LastMeterConsumption")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("LastMeterProduction")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("MaxPower")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MaxPowerProduction")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("MinPower")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("MinPowerProduction")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Power")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("PowerFactor")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("PowerProduction")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("PowerProductionReactive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("PowerReactive")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("SettingLockSeconds")
                        .HasColumnType("int");

                    b.Property<int>("SettingOffSetAvg")
                        .HasColumnType("int");

                    b.Property<int>("SettingPowerLoadSeconds")
                        .HasColumnType("int");

                    b.Property<int>("SettingToleranceAvg")
                        .HasColumnType("int");

                    b.Property<int?>("SignalStrength")
                        .HasColumnType("int");

                    b.Property<DateTime>("TS")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("VoltagePhase1")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("VoltagePhase2")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("VoltagePhase3")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Timestamp");

                    b.ToTable("RealTimeMeasurements");
                });

            modelBuilder.Entity("Growatt.OSS.Device", b =>
                {
                    b.Property<string>("DeviceSn")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("DeviceType")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("DeviceSn");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("EnergyAutomate.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("EnergyAutomate.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EnergyAutomate.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("EnergyAutomate.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
