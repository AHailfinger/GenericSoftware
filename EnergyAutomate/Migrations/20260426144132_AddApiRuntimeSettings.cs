using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class AddApiRuntimeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiRuntimeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ApiSettingAutoMode = table.Column<bool>(type: "bit", nullable: false),
                    ApiSettingAvgPower = table.Column<int>(type: "int", nullable: false),
                    ApiSettingAvgPowerHysteresis = table.Column<int>(type: "int", nullable: false),
                    ApiSettingAvgPowerLoadSeconds = table.Column<int>(type: "int", nullable: false),
                    ApiSettingAvgPowerOffset = table.Column<int>(type: "int", nullable: false),
                    ApiSettingBatteryPriorityMode = table.Column<bool>(type: "bit", nullable: false),
                    ApiSettingExtentionMode = table.Column<bool>(type: "bit", nullable: false),
                    ApiSettingExtentionAvgPower = table.Column<int>(type: "int", nullable: false),
                    ApiSettingExtentionExclusionFrom = table.Column<TimeSpan>(type: "time", nullable: false),
                    ApiSettingExtentionExclusionUntil = table.Column<TimeSpan>(type: "time", nullable: false),
                    ApiSettingMaxPower = table.Column<int>(type: "int", nullable: false),
                    ApiSettingPowerAdjustmentFactor = table.Column<int>(type: "int", nullable: false),
                    ApiSettingPowerAdjustmentWaitCycles = table.Column<int>(type: "int", nullable: false),
                    ApiSettingRestrictionMode = table.Column<bool>(type: "bit", nullable: false),
                    ApiSettingSocMax = table.Column<int>(type: "int", nullable: false),
                    ApiSettingSocMin = table.Column<int>(type: "int", nullable: false),
                    ApiSettingTimeOffset = table.Column<int>(type: "int", nullable: false),
                    ActiveCalculationTemplateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveAdjustmentTemplateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveDistributionTemplateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveDistributionManagerTemplateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRuntimeSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRuntimeSettings");
        }
    }
}
