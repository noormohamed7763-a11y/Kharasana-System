using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAndLockout : Migration
    {
        /// <summary>
        /// إنشاء فهرس فقط إذا لم يكن موجوداً مسبقاً.
        /// بعض قواعد البيانات (مثل التطوير) تحتوي فهارس مُضافة يدوياً لأغراض الأداء،
        /// لذا يُحاط إنشاء الفهرس بفحص يمنع تعارض "index/statistics already exists".
        /// </summary>
        private static void CreateIndexIfNotExists(
            MigrationBuilder migrationBuilder,
            string indexName,
            string table,
            string columns)
        {
            migrationBuilder.Sql(
                $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}' AND object_id = OBJECT_ID(N'{table}', N'U')) " +
                $"BEGIN CREATE INDEX [{indexName}] ON [{table}] ({columns}); END");
        }

        /// <summary>
        /// حذف الفهرس فقط إذا كان موجوداً — أمان عند التراجع.
        /// </summary>
        private static void DropIndexIfExists(
            MigrationBuilder migrationBuilder,
            string indexName,
            string table)
        {
            migrationBuilder.Sql(
                $"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}' AND object_id = OBJECT_ID(N'{table}', N'U')) " +
                $"DROP INDEX [{indexName}] ON [{table}];");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Factories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            CreateIndexIfNotExists(migrationBuilder, "IX_Users_Role", "Users", "[Role]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Users_Role_FactoryId", "Users", "[Role], [FactoryId]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Orders_CreatedAt", "Orders", "[CreatedAt]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Orders_FactoryId_Status", "Orders", "[FactoryId], [Status]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Orders_Status", "Orders", "[Status]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Factories_IsDeleted", "Factories", "[IsDeleted]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfExists(migrationBuilder, "IX_Users_Role", "Users");
            DropIndexIfExists(migrationBuilder, "IX_Users_Role_FactoryId", "Users");
            DropIndexIfExists(migrationBuilder, "IX_Orders_CreatedAt", "Orders");
            DropIndexIfExists(migrationBuilder, "IX_Orders_FactoryId_Status", "Orders");
            DropIndexIfExists(migrationBuilder, "IX_Orders_Status", "Orders");
            DropIndexIfExists(migrationBuilder, "IX_Factories_IsDeleted", "Factories");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Factories");
        }
    }
}