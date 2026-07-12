using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerValueBattery",
                table: "GrowattDevices",
                newName: "PowerValueBatteryStatus");

            migrationBuilder.AddColumn<int>(
                name: "PowerValueBatteryPower",
                table: "GrowattDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerValueBatteryPower",
                table: "GrowattDevices");

            migrationBuilder.RenameColumn(
                name: "PowerValueBatteryStatus",
                table: "GrowattDevices",
                newName: "PowerValueBattery");
        }
    }
}
