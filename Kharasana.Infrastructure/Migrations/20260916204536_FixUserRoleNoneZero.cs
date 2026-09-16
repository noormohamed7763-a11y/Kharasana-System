using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kharasana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserRoleNoneZero : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// إصلاح أمني: كانت القيمة الافتراضية 0 تُفسَّر كـ Admin، فأي صف جديد بلا قيمة يدخل
        /// بصلاحيات مدير. أصبح 0 = None، والأدوار الفعلية تبدأ من 1.
        /// القيم السابقة (0..3) تُنقل إلى (1..4) بنفس ترتيبها الهرمي.
        /// لا يتصل هذا برموز JWT — الـ token يحمل الاسم (Role.ToString()) لا الرقم.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // في SQL Server، UPDATE يقرأ كل الصفوف من لقطة قبل التعديل،
            // لذا 0→1 ثم 1→2… لن تتزاحم أثناء التنفيذ — آمن بلا CASE.
            // المدى بعد التطبيق: Admin=1, FactoryEmployee=2, Driver=3, Client=4.
            migrationBuilder.Sql(
                """
                UPDATE Users SET Role = Role + 1;
                """,
                suppressTransaction: false);
        }

        /// <inheritdoc />
        /// <remarks>
        /// متعمّداً لا يُستعاد: بعد قبول القيم الجديدة لن يوجد أي صف قيمته 0 (None)،
        /// وعكس الطرح على data فعلية يُهبط Admin (1) إلى 0 (None) فيُفقد صلاحياته.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "لا يمكن الرجوع عن إصلاح دور Admin: النطاق الجديد (1..4) لا يُستعاد بأمان " +
                "(ستُهبط أدوار Admin إلى القيمة 0 = None). أنشئ تصحيحاً يدوياً إن لزم.");
        }
    }
}
