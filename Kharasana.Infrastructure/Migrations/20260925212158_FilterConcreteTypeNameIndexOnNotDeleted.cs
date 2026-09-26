using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <summary>
    /// تحويل فهرس التفرّد على أنواع الخرسانة من غير مُرشَّح إلى مُرشَّح بـ <c>WHERE [IsDeleted] = 0</c>،
    /// فيصبح القيد فعّالاً على الأنواع غير المحذوفة فقط ويُتحرَّر الاسم فور الحذف الناعم.
    /// </summary>
    /// <remarks>
    /// الفهرس يُستبدل لا يُضاف: الاسم <c>IX_ConcreteTypes_FactoryId_Name</c> نفسه،
    /// لذا يجب إسقاطه أولاً حتى لا ينشأ فهرسان متعارضان على العمودين نفسيهما.
    /// الإسقاط والإنشاء محاطان بفحص وجود لأن قواعد التطوير تحتوي فهارس مُضافة يدوياً
    /// قد لا يعرفها سجل الترحيلات، وفشل "index already exists" يُبطل الترحيل كاملاً.
    /// ملاحظة: لم يُنفَّذ هذا الترحيل على SQL Server فعليًا — التحقق الموثّق مؤجَّل
    /// (انظر SQL_SERVER_INTEGRATION_TESTING.md).
    /// </remarks>
    public partial class FilterConcreteTypeNameIndexOnNotDeleted : Migration
    {
        private const string IndexName = "IX_ConcreteTypes_FactoryId_Name";
        private const string TableName = "ConcreteTypes";

        /// <summary>إسقاط الفهرس فقط إن كان موجوداً — يمنع فشل الترحيل على قاعدة نظيفة أو مُعدَّلة يدويًا.</summary>
        private static void DropIndexIfExists(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{IndexName}' AND object_id = OBJECT_ID(N'{TableName}', N'U')) " +
                $"DROP INDEX [{IndexName}] ON [{TableName}];");
        }

        /// <summary>إنشاء الفهرس المُرشَّح فقط إن لم يكن موجوداً بنفس الاسم.</summary>
        private static void CreateFilteredIndexIfNotExists(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{IndexName}' AND object_id = OBJECT_ID(N'{TableName}', N'U')) " +
                $"BEGIN CREATE UNIQUE INDEX [{IndexName}] ON [{TableName}] ([FactoryId], [Name]) WHERE [IsDeleted] = 0; END");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إسقاط الفهرس القديم غير المُرشَّح ثم إعادة إنشائه مُرشَّحاً بنفس الاسم
            DropIndexIfExists(migrationBuilder);
            CreateFilteredIndexIfNotExists(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfExists(migrationBuilder);

            // إعادة الفهرس غير المُرشَّح (السلوك السابق) — يُفشل التراجع إن كان هناك
            // اسم مكرر بين نوع نشط وآخر محذوف، لأن الفهرس القديم يحجز الأسماء المحذوفة.
            migrationBuilder.Sql(
                $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{IndexName}' AND object_id = OBJECT_ID(N'{TableName}', N'U')) " +
                $"BEGIN CREATE UNIQUE INDEX [{IndexName}] ON [{TableName}] ([FactoryId], [Name]); END");
        }
    }
}
