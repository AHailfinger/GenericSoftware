using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTables3 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgLastPowerValue",
                table: "RealTimeMeasurements",
                newName: "PowerOutValue");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerOutValue",
                table: "RealTimeMeasurements",
                newName: "AvgLastPowerValue");
        }

        #endregion Protected Methods
    }
}
