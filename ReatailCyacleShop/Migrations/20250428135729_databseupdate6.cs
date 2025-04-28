using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCycleShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class databseupdate6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "InvitedUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "phoneNumber",
                table: "InvitedUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "InvitedUsers");

            migrationBuilder.DropColumn(
                name: "phoneNumber",
                table: "InvitedUsers");
        }
    }
}
