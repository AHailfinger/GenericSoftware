using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable35 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SettingPowerLoadSeconds",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SettingOffSetAvg",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SettingAvgPowerHysteresis",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalRequested",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalDefault",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalCommited",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PowerAvgProduction",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PowerAvgConsumption",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PowerValueBattery",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueOutput",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueSolar",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueBattery",
                table: "GrowattDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueDefault",
                table: "GrowattDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueOutput",
                table: "GrowattDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueSolar",
                table: "GrowattDevices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerValueBattery",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "PowerValueOutput",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "PowerValueSolar",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "PowerValueBattery",
                table: "GrowattDevices");

            migrationBuilder.DropColumn(
                name: "PowerValueDefault",
                table: "GrowattDevices");

            migrationBuilder.DropColumn(
                name: "PowerValueOutput",
                table: "GrowattDevices");

            migrationBuilder.DropColumn(
                name: "PowerValueSolar",
                table: "GrowattDevices");

            migrationBuilder.AlterColumn<int>(
                name: "SettingPowerLoadSeconds",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SettingOffSetAvg",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SettingAvgPowerHysteresis",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalRequested",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalDefault",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PowerValueTotalCommited",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PowerAvgProduction",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PowerAvgConsumption",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
