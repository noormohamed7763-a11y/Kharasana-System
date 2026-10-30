using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Admixture",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AggregateSize",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CementType",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorPhone",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    TruckPlate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasPump = table.Column<bool>(type: "bit", nullable: false),
                    PumpDriverId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnloadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_Trips_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "FactoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Users_PumpDriverId",
                        column: x => x.PumpDriverId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DriverId",
                table: "Trips",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_FactoryId",
                table: "Trips",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OrderId",
                table: "Trips",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_PumpDriverId",
                table: "Trips",
                column: "PumpDriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropColumn(
                name: "Admixture",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AggregateSize",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CementType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SupervisorPhone",
                table: "Orders");
        }
    }
}
