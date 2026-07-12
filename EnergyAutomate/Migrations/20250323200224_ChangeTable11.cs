using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable11 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommitedPowerValue",
                table: "RealTimeMeasurements");

            migrationBuilder.DropColumn(
                name: "RequestedPowerValue",
                table: "RealTimeMeasurements");

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfos",
                table: "RealTimeMeasurements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceInfos",
                table: "RealTimeMeasurements");

            migrationBuilder.AddColumn<int>(
                name: "CommitedPowerValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedPowerValue",
                table: "RealTimeMeasurements",
                type: "int",
                nullable: true);
        }

        #endregion Protected Methods
    }
}
