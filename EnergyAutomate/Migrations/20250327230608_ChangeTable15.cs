using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable15 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerValueRequested",
                table: "Devices",
                newName: "RequestedPowerValue");

            migrationBuilder.RenameColumn(
                name: "PowerValueLastChanged",
                table: "Devices",
                newName: "LastChangePowerValue");

            migrationBuilder.RenameColumn(
                name: "PowerValueCommited",
                table: "Devices",
                newName: "CommitedPowerValue");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestedPowerValue",
                table: "Devices",
                newName: "PowerValueRequested");

            migrationBuilder.RenameColumn(
                name: "LastChangePowerValue",
                table: "Devices",
                newName: "PowerValueLastChanged");

            migrationBuilder.RenameColumn(
                name: "CommitedPowerValue",
                table: "Devices",
                newName: "PowerValueCommited");
        }

        #endregion Protected Methods
    }
}
