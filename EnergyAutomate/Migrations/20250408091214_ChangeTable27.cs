using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiPenaltyFrequentlyAccess",
                table: "TibberRealTimeMeasurements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiPenaltyFrequentlyAccess",
                table: "TibberRealTimeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
