using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailCycleShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class payment1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePaymentId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_InvitedUsers_Email",
                table: "InvitedUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitedUsers_Token",
                table: "InvitedUsers",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvitedUsers_Email",
                table: "InvitedUsers");

            migrationBuilder.DropIndex(
                name: "IX_InvitedUsers_Token",
                table: "InvitedUsers");

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentId",
                table: "Payments",
                type: "text",
                nullable: true);
        }
    }
}
