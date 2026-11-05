using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VerifiedByUserId",
                table: "OrderPayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultEntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultPayload = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_VerifiedByUserId",
                table: "OrderPayments",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_UserId_Key",
                table: "IdempotencyRecords",
                columns: new[] { "UserId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPayments_Users_VerifiedByUserId",
                table: "OrderPayments",
                column: "VerifiedByUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPayments_Users_VerifiedByUserId",
                table: "OrderPayments");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_VerifiedByUserId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "OrderPayments");
        }
    }
}
