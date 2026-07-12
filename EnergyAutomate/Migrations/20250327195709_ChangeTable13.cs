using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable13 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgPowerConsumption",
                table: "RealTimeMeasurements");

            migrationBuilder.RenameColumn(
                name: "AvgPowerProduction",
                table: "RealTimeMeasurements",
                newName: "AvgPowerLoad");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgPowerLoad",
                table: "RealTimeMeasurements",
                newName: "AvgPowerProduction");

            migrationBuilder.AddColumn<int>(
                name: "AvgPowerConsumption",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        #endregion Protected Methods
    }
}
