using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable16 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettingAutoModeRestriction",
                table: "RealTimeMeasurements");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SettingAutoModeRestriction",
                table: "RealTimeMeasurements",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        #endregion Protected Methods
    }
}
