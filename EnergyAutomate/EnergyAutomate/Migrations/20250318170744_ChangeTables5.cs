using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTables5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgTotalValue",
                table: "RealTimeMeasurements",
                newName: "SettingPowerLoadSeconds");

            migrationBuilder.RenameColumn(
                name: "AvgPowerLoadValue",
                table: "RealTimeMeasurements",
                newName: "SettingOffSetAvg");

            migrationBuilder.RenameColumn(
                name: "AvgOffSet",
                table: "RealTimeMeasurements",
                newName: "SettingLockSeconds");

            migrationBuilder.RenameColumn(
                name: "AvgLastPowerValue",
                table: "RealTimeMeasurements",
                newName: "AvgPowerLoad");

            migrationBuilder.AddColumn<int>(
                name: "AvgOutputValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgOutputValue",
                table: "RealTimeMeasurements");

            migrationBuilder.RenameColumn(
                name: "SettingPowerLoadSeconds",
                table: "RealTimeMeasurements",
                newName: "AvgTotalValue");

            migrationBuilder.RenameColumn(
                name: "SettingOffSetAvg",
                table: "RealTimeMeasurements",
                newName: "AvgPowerLoadValue");

            migrationBuilder.RenameColumn(
                name: "SettingLockSeconds",
                table: "RealTimeMeasurements",
                newName: "AvgOffSet");

            migrationBuilder.RenameColumn(
                name: "AvgPowerLoad",
                table: "RealTimeMeasurements",
                newName: "AvgLastPowerValue");
        }
    }
}
