using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnergyAutomate.Migrations
{
    /// <inheritdoc/>
    public partial class TableChange23 : Migration
    {
        #region Protected Methods

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GrowattElements",
                table: "GrowattElements");

            migrationBuilder.RenameTable(
                name: "GrowattElements",
                newName: "GrowattElement");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "GrowattElement",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrowattElement",
                table: "GrowattElement",
                column: "Id");
        }

        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GrowattElement",
                table: "GrowattElement");

            migrationBuilder.RenameTable(
                name: "GrowattElement",
                newName: "GrowattElements");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "GrowattElements",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GrowattElements",
                table: "GrowattElements",
                column: "Id");
        }

        #endregion Protected Methods
    }
}
