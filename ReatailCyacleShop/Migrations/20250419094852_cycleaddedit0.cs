using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCycleShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class cycleaddedit0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "Cycles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventoryId",
                table: "Cycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
