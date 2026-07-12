using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable18 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettingAutoMode",
                table: "RealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "SettingBatteryPriorityMode",
                table: "RealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "SettingRestrictionMode",
                table: "RealTimeMeasurements");

            migrationBuilder.RenameColumn(
                name: "SettingRestrictionState",
                table: "RealTimeMeasurements",
                newName: "SettingAutoModeRestriction");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SettingAutoModeRestriction",
                table: "RealTimeMeasurements",
                newName: "SettingRestrictionState");

            migrationBuilder.AddColumn<bool>(
                name: "SettingAutoMode",
                table: "RealTimeMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SettingBatteryPriorityMode",
                table: "RealTimeMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SettingRestrictionMode",
                table: "RealTimeMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        #endregion Protected Methods
    }
}
