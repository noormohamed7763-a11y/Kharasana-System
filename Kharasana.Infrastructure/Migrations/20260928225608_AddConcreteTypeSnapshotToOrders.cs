using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConcreteTypeSnapshotToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcreteTypeNameSnapshot",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConcreteTypeStrengthSnapshot",
                table: "Orders",
                type: "int",
                nullable: true);

            // ✅ تعبئة خلفية للطلبات القائمة: اللقطتان تُكتبان من الكتالوج الحالي.
            //    هذا أفضل تقدير متاح لصفٍّ سابق للترحيل، ولا مفرّ منه لأن الاسم الأصلي
            //    لم يكن محفوظًا في أي موضع. ولا يُطبَّق فلتر الحذف الناعم هنا (SQL خام)
            //    وهذا مقصود: نوع مؤرشف يبقى مصدرًا صالحًا لاسم طلبه التاريخي.
            //    وشرط IS NULL يجعلها آمنة لو أُعيد تشغيلها فلا تدهس لقطة قائمة.
            migrationBuilder.Sql("""
                UPDATE o
                SET o.[ConcreteTypeNameSnapshot] = c.[Name],
                    o.[ConcreteTypeStrengthSnapshot] = c.[Strength]
                FROM [Orders] AS o
                INNER JOIN [ConcreteTypes] AS c ON c.[ConcreteTypeId] = o.[ConcreteTypeId]
                WHERE o.[ConcreteTypeNameSnapshot] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcreteTypeNameSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConcreteTypeStrengthSnapshot",
                table: "Orders");
        }
    }
}
