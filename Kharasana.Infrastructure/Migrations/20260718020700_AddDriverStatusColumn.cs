using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConcreteTypes_FactoryId",
                table: "ConcreteTypes");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<int>(
                name: "DriverStatus",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransportMethod",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factories_FactoryName",
                table: "Factories",
                column: "FactoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteTypes_FactoryId_Name",
                table: "ConcreteTypes",
                columns: new[] { "FactoryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Phone",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Factories_FactoryName",
                table: "Factories");

            migrationBuilder.DropIndex(
                name: "IX_ConcreteTypes_FactoryId_Name",
                table: "ConcreteTypes");

            migrationBuilder.DropColumn(
                name: "DriverStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TransportMethod",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConcreteTypes_FactoryId",
                table: "ConcreteTypes",
                column: "FactoryId");
        }
    }
}
