using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TightenOrderPaymentColumnsAndLedgerPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "OrderPayments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "OrderPayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // ✅ تعبئة رجعية لـ NetAmount.
            //    العمود أُضيف بالقيمة الافتراضية 0، ولم يكن أي مسار في الكود يعيّنه
            //    (فُدِر من قاعدة البيانات وحدها) — فكل الطلبات القائمة صافيها صفر،
            //    و<c>RemainingAmount = NetAmount − PaidAmount</c> يعرض رصيداً سالباً
            //    كاذباً على العميل.
            //    الشرط <c>NetAmount = 0</c> يحصر التعبئة في الصفوف التي لم تُحسب
            //    بعد، فلا يمسّ طلباً صُحِّح يدوياً.
            migrationBuilder.Sql(
                "UPDATE [Orders] SET [NetAmount] = [TotalPrice] - [TotalDiscountAmount] " +
                "WHERE [NetAmount] = 0 AND [TotalPrice] <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "OrderPayments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "OrderPayments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
