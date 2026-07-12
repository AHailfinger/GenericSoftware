using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveElementIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
