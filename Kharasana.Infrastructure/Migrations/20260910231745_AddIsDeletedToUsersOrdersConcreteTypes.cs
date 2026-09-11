using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToUsersOrdersConcreteTypes : Migration
    {
        /// <summary>
        /// إنشاء أي فهرس إضافي فقط إذا لم يكن موجوداً مسبقاً.
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
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ConcreteTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // فهارس داعمة لفلاتر الحذف الناعم العامة (!IsDeleted)
            CreateIndexIfNotExists(migrationBuilder, "IX_Users_IsDeleted", "Users", "[IsDeleted]");
            CreateIndexIfNotExists(migrationBuilder, "IX_Orders_IsDeleted", "Orders", "[IsDeleted]");
            CreateIndexIfNotExists(migrationBuilder, "IX_ConcreteTypes_IsDeleted", "ConcreteTypes", "[IsDeleted]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfExists(migrationBuilder, "IX_Users_IsDeleted", "Users");
            DropIndexIfExists(migrationBuilder, "IX_Orders_IsDeleted", "Orders");
            DropIndexIfExists(migrationBuilder, "IX_ConcreteTypes_IsDeleted", "ConcreteTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ConcreteTypes");
        }
    }
}
