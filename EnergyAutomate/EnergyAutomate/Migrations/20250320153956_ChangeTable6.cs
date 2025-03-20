using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgOutputValue",
                table: "RealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "AvgPowerValue",
                table: "RealTimeMeasurements");

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfos",
                table: "RealTimeMeasurements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceInfos",
                table: "RealTimeMeasurements");

            migrationBuilder.AddColumn<int>(
                name: "AvgOutputValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgPowerValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
