using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedConcreteTypes",
                table: "FactoryRegistrationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionBatches",
                columns: table => new
                {
                    ProductionBatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ConcreteTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatches", x => x.ProductionBatchId);
                    table.ForeignKey(
                        name: "FK_ProductionBatches_ConcreteTypes_ConcreteTypeId",
                        column: x => x.ConcreteTypeId,
                        principalTable: "ConcreteTypes",
                        principalColumn: "ConcreteTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionBatches_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "FactoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionBatches_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    RawMaterialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.RawMaterialId);
                    table.ForeignKey(
                        name: "FK_RawMaterials_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "FactoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ConcreteTypeId",
                table: "ProductionBatches",
                column: "ConcreteTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_FactoryId",
                table: "ProductionBatches",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_FactoryId_Status_Filtered",
                table: "ProductionBatches",
                columns: new[] { "FactoryId", "Status" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_OrderId",
                table: "ProductionBatches",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_FactoryId",
                table: "RawMaterials",
                column: "FactoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionBatches");

            migrationBuilder.DropTable(
                name: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "RequestedConcreteTypes",
                table: "FactoryRegistrationRequests");
        }
    }
}
