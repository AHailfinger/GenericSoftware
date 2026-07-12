using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable26 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestedPowerValue",
                table: "TibberRealTimeMeasurements",
                newName: "PowerValueTotalRequested");

            migrationBuilder.RenameColumn(
                name: "PenaltyFrequentlyAccess",
                table: "TibberRealTimeMeasurements",
                newName: "PowerAvgProduction");

            migrationBuilder.RenameColumn(
                name: "CommitedPowerValue",
                table: "TibberRealTimeMeasurements",
                newName: "PowerValueTotalCommited");

            migrationBuilder.RenameColumn(
                name: "AvgPowerProduction",
                table: "TibberRealTimeMeasurements",
                newName: "PowerAvgConsumption");

            migrationBuilder.RenameColumn(
                name: "AvgPowerConsumption",
                table: "TibberRealTimeMeasurements",
                newName: "ApiPenaltyFrequentlyAccess");

            migrationBuilder.AddColumn<int>(
                name: "PowerValueNewCommited",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PowerValueNewDeviceSn",
                table: "TibberRealTimeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerValueNewRequested",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerValueNewCommited",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "PowerValueNewDeviceSn",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "PowerValueNewRequested",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.RenameColumn(
                name: "PowerValueTotalRequested",
                table: "TibberRealTimeMeasurements",
                newName: "RequestedPowerValue");

            migrationBuilder.RenameColumn(
                name: "PowerValueTotalCommited",
                table: "TibberRealTimeMeasurements",
                newName: "CommitedPowerValue");

            migrationBuilder.RenameColumn(
                name: "PowerAvgProduction",
                table: "TibberRealTimeMeasurements",
                newName: "PenaltyFrequentlyAccess");

            migrationBuilder.RenameColumn(
                name: "PowerAvgConsumption",
                table: "TibberRealTimeMeasurements",
                newName: "AvgPowerProduction");

            migrationBuilder.RenameColumn(
                name: "ApiPenaltyFrequentlyAccess",
                table: "TibberRealTimeMeasurements",
                newName: "AvgPowerConsumption");
        }
    }
}
