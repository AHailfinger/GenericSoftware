using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceNoahInfo",
                columns: table => new
                {
                    DeviceSn = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DatalogSn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssociatedInvSn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lost = table.Column<bool>(type: "bit", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<long>(type: "bigint", nullable: false),
                    SysTime = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChargingSocHighLimit = table.Column<int>(type: "int", nullable: false),
                    ChargingSocLowLimit = table.Column<int>(type: "int", nullable: false),
                    DefaultPower = table.Column<int>(type: "int", nullable: false),
                    ComponentPower = table.Column<double>(type: "float", nullable: false),
                    Time1Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time1End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time1Mode = table.Column<int>(type: "int", nullable: false),
                    Time1Power = table.Column<int>(type: "int", nullable: false),
                    Time2Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time2End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time2Mode = table.Column<int>(type: "int", nullable: false),
                    Time2Power = table.Column<int>(type: "int", nullable: false),
                    Time3Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time3End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time3Mode = table.Column<int>(type: "int", nullable: false),
                    Time3Power = table.Column<int>(type: "int", nullable: false),
                    Time4Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time4End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time4Mode = table.Column<int>(type: "int", nullable: false),
                    Time4Power = table.Column<int>(type: "int", nullable: false),
                    Time5Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time5End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time5Mode = table.Column<int>(type: "int", nullable: false),
                    Time5Power = table.Column<int>(type: "int", nullable: false),
                    Time6Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time6End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time6Mode = table.Column<int>(type: "int", nullable: false),
                    Time6Power = table.Column<int>(type: "int", nullable: false),
                    Time7Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time7End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time7Mode = table.Column<int>(type: "int", nullable: false),
                    Time7Power = table.Column<int>(type: "int", nullable: false),
                    Time8Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time8End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time8Mode = table.Column<int>(type: "int", nullable: false),
                    Time8Power = table.Column<int>(type: "int", nullable: false),
                    Time9Start = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time9End = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time9Mode = table.Column<int>(type: "int", nullable: false),
                    Time9Power = table.Column<int>(type: "int", nullable: false),
                    Time1Enable = table.Column<int>(type: "int", nullable: false),
                    Time2Enable = table.Column<int>(type: "int", nullable: false),
                    Time3Enable = table.Column<int>(type: "int", nullable: false),
                    Time4Enable = table.Column<int>(type: "int", nullable: false),
                    Time5Enable = table.Column<int>(type: "int", nullable: false),
                    Time6Enable = table.Column<int>(type: "int", nullable: false),
                    Time7Enable = table.Column<int>(type: "int", nullable: false),
                    Time8Enable = table.Column<int>(type: "int", nullable: false),
                    Time9Enable = table.Column<int>(type: "int", nullable: false),
                    SmartSocketPower = table.Column<double>(type: "float", nullable: false),
                    OtaDeviceTypeCodeHigh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtaDeviceTypeCodeLow = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FwVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MpptVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BmsVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EbmOrderNum = table.Column<int>(type: "int", nullable: false),
                    TempType = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTimeText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNoahInfo", x => x.DeviceSn);
                });

            migrationBuilder.CreateTable(
                name: "DeviceNoahLastData",
                columns: table => new
                {
                    deviceSn = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    time = table.Column<long>(type: "bigint", nullable: false),
                    datalogSn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isAgain = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    mpptProtectStatus = table.Column<int>(type: "int", nullable: false),
                    pdWarnStatus = table.Column<int>(type: "int", nullable: false),
                    pac = table.Column<float>(type: "real", nullable: false),
                    eacToday = table.Column<float>(type: "real", nullable: false),
                    eacMonth = table.Column<float>(type: "real", nullable: false),
                    eacYear = table.Column<float>(type: "real", nullable: false),
                    eacTotal = table.Column<float>(type: "real", nullable: false),
                    ppv = table.Column<float>(type: "real", nullable: false),
                    workMode = table.Column<int>(type: "int", nullable: false),
                    totalBatteryPackChargingStatus = table.Column<int>(type: "int", nullable: false),
                    totalBatteryPackChargingPower = table.Column<int>(type: "int", nullable: false),
                    batteryPackageQuantity = table.Column<int>(type: "int", nullable: false),
                    totalBatteryPackSoc = table.Column<int>(type: "int", nullable: false),
                    heatingStatus = table.Column<int>(type: "int", nullable: false),
                    faultStatus = table.Column<int>(type: "int", nullable: false),
                    battery1SerialNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    battery1Soc = table.Column<int>(type: "int", nullable: false),
                    battery1Temp = table.Column<float>(type: "real", nullable: false),
                    battery1WarnStatus = table.Column<int>(type: "int", nullable: false),
                    battery1ProtectStatus = table.Column<int>(type: "int", nullable: false),
                    battery2SerialNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    battery2Soc = table.Column<int>(type: "int", nullable: false),
                    battery2Temp = table.Column<float>(type: "real", nullable: false),
                    battery2WarnStatus = table.Column<int>(type: "int", nullable: false),
                    battery2ProtectStatus = table.Column<int>(type: "int", nullable: false),
                    battery3SerialNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    battery3Soc = table.Column<int>(type: "int", nullable: false),
                    battery3Temp = table.Column<float>(type: "real", nullable: false),
                    battery3WarnStatus = table.Column<int>(type: "int", nullable: false),
                    battery3ProtectStatus = table.Column<int>(type: "int", nullable: false),
                    battery4SerialNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    battery4Soc = table.Column<int>(type: "int", nullable: false),
                    battery4Temp = table.Column<float>(type: "real", nullable: false),
                    battery4WarnStatus = table.Column<int>(type: "int", nullable: false),
                    battery4ProtectStatus = table.Column<int>(type: "int", nullable: false),
                    settableTimePeriod = table.Column<int>(type: "int", nullable: false),
                    acCoupleWarnStatus = table.Column<int>(type: "int", nullable: false),
                    acCoupleProtectStatus = table.Column<int>(type: "int", nullable: false),
                    ctFlag = table.Column<int>(type: "int", nullable: false),
                    totalHouseholdLoad = table.Column<float>(type: "real", nullable: false),
                    householdLoadApartFromGroplug = table.Column<float>(type: "real", nullable: false),
                    onOffGrid = table.Column<int>(type: "int", nullable: false),
                    ctSelfPower = table.Column<float>(type: "real", nullable: false),
                    chargeSocLimit = table.Column<int>(type: "int", nullable: false),
                    dischargeSocLimit = table.Column<int>(type: "int", nullable: false),
                    pv1Voltage = table.Column<float>(type: "real", nullable: false),
                    pv1Current = table.Column<float>(type: "real", nullable: false),
                    pv1Temp = table.Column<float>(type: "real", nullable: false),
                    pv2Voltage = table.Column<float>(type: "real", nullable: false),
                    pv2Current = table.Column<float>(type: "real", nullable: false),
                    pv2Temp = table.Column<float>(type: "real", nullable: false),
                    systemTemp = table.Column<float>(type: "real", nullable: false),
                    maxCellVoltage = table.Column<float>(type: "real", nullable: false),
                    minCellVoltage = table.Column<float>(type: "real", nullable: false),
                    batteryCycles = table.Column<int>(type: "int", nullable: false),
                    batterySoh = table.Column<int>(type: "int", nullable: false),
                    pv3Voltage = table.Column<float>(type: "real", nullable: false),
                    pv3Current = table.Column<float>(type: "real", nullable: false),
                    pv3Temp = table.Column<float>(type: "real", nullable: false),
                    pv4Voltage = table.Column<float>(type: "real", nullable: false),
                    pv4Current = table.Column<float>(type: "real", nullable: false),
                    pv4Temp = table.Column<float>(type: "real", nullable: false),
                    battery1TempF = table.Column<float>(type: "real", nullable: false),
                    battery2TempF = table.Column<float>(type: "real", nullable: false),
                    battery3TempF = table.Column<float>(type: "real", nullable: false),
                    battery4TempF = table.Column<float>(type: "real", nullable: false),
                    timeStr = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNoahLastData", x => new { x.deviceSn, x.time });
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceSn = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceSn);
                });

            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.StartsAt);
                });

            migrationBuilder.CreateTable(
                name: "RealTimeMeasurements",
                columns: table => new
                {
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AvgOffSet = table.Column<int>(type: "int", nullable: false),
                    PowerOutValue = table.Column<int>(type: "int", nullable: false),
                    AvgTotalValue = table.Column<int>(type: "int", nullable: false),
                    Power = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastMeterConsumption = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AccumulatedConsumption = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedConsumptionLastHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedProductionLastHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccumulatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AccumulatedReward = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinPower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AveragePower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxPower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PowerProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PowerReactive = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PowerProductionReactive = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinPowerProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPowerProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LastMeterProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoltagePhase1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoltagePhase2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoltagePhase3 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentPhase1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentPhase2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentPhase3 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PowerFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SignalStrength = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealTimeMeasurements", x => x.Timestamp);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DeviceNoahInfo");

            migrationBuilder.DropTable(
                name: "DeviceNoahLastData");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Prices");

            migrationBuilder.DropTable(
                name: "RealTimeMeasurements");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
