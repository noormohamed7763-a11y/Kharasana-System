using Microsoft.EntityFrameworkCore.Migrations;

namespace Kharasana.Infrastructure.Migrations
{
    /// <summary>
    /// Add VerifiedByUserId to OrderPayments
    /// </summary>
    public partial class AddOrderPaymentVerifiedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int?>(
                name: "VerifiedByUserId",
                table: "OrderPayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_VerifiedByUserId",
                table: "OrderPayments",
                column: "VerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPayments_Users_VerifiedByUserId",
                table: "OrderPayments",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPayments_Users_VerifiedByUserId",
                table: "OrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_VerifiedByUserId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "OrderPayments");
        }
    }
}
