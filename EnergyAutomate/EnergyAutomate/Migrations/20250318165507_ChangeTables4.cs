using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTables4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgPowerLoad",
                table: "RealTimeMeasurements",
                newName: "AvgPowerValue");

            migrationBuilder.AddColumn<int>(
                name: "AvgPowerLoadValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgPowerLoadValue",
                table: "RealTimeMeasurements");

            migrationBuilder.RenameColumn(
                name: "AvgPowerValue",
                table: "RealTimeMeasurements",
                newName: "AvgPowerLoad");
        }
    }
}
