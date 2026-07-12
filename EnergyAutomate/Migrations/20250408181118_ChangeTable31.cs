using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTable31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiAdjustmentValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiAdjustmentValues",
                columns: table => new
                {
                    TS = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceSn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PowerValueCommited = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PowerValueRequested = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TSCommited = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiAdjustmentValues", x => x.TS);
                });
        }
    }
}
