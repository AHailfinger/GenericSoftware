using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class ChangeTable10 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoModeRestriction",
                table: "Prices");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoModeRestriction",
                table: "Prices",
                type: "bit",
                nullable: true);
        }

        #endregion Protected Methods
    }
}
