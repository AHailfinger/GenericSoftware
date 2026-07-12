using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable28 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TibberRealTimeMeasurements",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TS",
                table: "TibberRealTimeMeasurements",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TibberRealTimeMeasurements",
                table: "TibberRealTimeMeasurements",
                column: "TS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TibberRealTimeMeasurements",
                table: "TibberRealTimeMeasurements");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TS",
                table: "TibberRealTimeMeasurements",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TibberRealTimeMeasurements",
                table: "TibberRealTimeMeasurements",
                column: "Timestamp");
        }
    }
}
