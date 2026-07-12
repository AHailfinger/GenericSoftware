using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGrowattElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrowattElements");

            migrationBuilder.DropColumn(
                name: "ActiveAdjustmentElementId",
                table: "ApiRuntimeSettings");

            migrationBuilder.DropColumn(
                name: "ActiveCalculationElementId",
                table: "ApiRuntimeSettings");

            migrationBuilder.DropColumn(
                name: "ActiveDistributionElementId",
                table: "ApiRuntimeSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveAdjustmentElementId",
                table: "ApiRuntimeSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveCalculationElementId",
                table: "ApiRuntimeSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveDistributionElementId",
                table: "ApiRuntimeSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GrowattElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElementType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowattElements", x => x.Id);
                });
        }
    }
}
