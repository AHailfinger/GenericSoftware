using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTables2 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgPowerLoad",
                table: "RealTimeMeasurements",
                newName: "CurrentPowerLoad");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrentPowerLoad",
                table: "RealTimeMeasurements",
                newName: "AvgPowerLoad");
        }

        #endregion Protected Methods
    }
}
