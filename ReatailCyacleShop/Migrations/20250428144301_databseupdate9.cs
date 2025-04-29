using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCycleShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class databseupdate9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "phoneNumber",
                table: "adminCreateUserModels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phoneNumber",
                table: "adminCreateUserModels",
                type: "text",
                nullable: true);
        }
    }
}
