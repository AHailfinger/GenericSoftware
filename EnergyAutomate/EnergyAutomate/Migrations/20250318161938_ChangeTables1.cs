using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTables1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentPowerLoad",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPowerLoad",
                table: "RealTimeMeasurements");
        }
    }
}
