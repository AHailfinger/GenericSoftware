using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable17 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SettingAvgPowerHysteresis",
                table: "RealTimeMeasurements",
                newName: "SettingToleranceAvg");

            migrationBuilder.RenameColumn(
                name: "PenaltyFrequentlyAccess",
                table: "RealTimeMeasurements",
                newName: "SettingLockSeconds");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SettingToleranceAvg",
                table: "RealTimeMeasurements",
                newName: "SettingAvgPowerHysteresis");

            migrationBuilder.RenameColumn(
                name: "SettingLockSeconds",
                table: "RealTimeMeasurements",
                newName: "PenaltyFrequentlyAccess");
        }

        #endregion Protected Methods
    }
}
