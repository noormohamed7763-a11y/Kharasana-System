using Kharasana.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات UniqueConstraintDetector — تضمن أن التقاط انتهاك التفرّد ضيّق لا عام:
/// 409 للتكرار فقط، بينما أخطاء المفاتيح الأجنبية وبقية أخطاء قاعدة البيانات لا تُترجم.
/// ملاحظة: إنشاء SqlException حقيقي غير ممكن بدون SQL Server، لذا يغطّي هذا الملف
/// أرقام الأخطاء والاستثناءات غير التابعة لـ SQL Server؛ أما الإنفاذ الفعلي للفهرس
/// فيحتاج اختبار تكامل على SQL Server حقيقي.
/// </summary>
public class UniqueConstraintDetectorTests
{
    [Theory]
    [InlineData(2601)] // محاولة إدراج مفتاح مكرر في فهرس فريد
    [InlineData(2627)] // انتهاك قيد UNIQUE
    public void IsUniqueViolation_Number_DetectsUniqueConstraintErrors(int errorNumber)
        => UniqueConstraintDetector.IsUniqueViolation(errorNumber).Should().BeTrue();

    [Theory]
    [InlineData(547)]  // انتهاك مفتاح أجنبي / CHECK — ليس تكرارًا
    [InlineData(515)]  // إدراج NULL في عمود لا يقبل NULL
    [InlineData(1205)] // جمود (Deadlock)
    [InlineData(208)]  // جدول غير موجود
    [InlineData(0)]
    public void IsUniqueViolation_Number_IgnoresUnrelatedDatabaseErrors(int errorNumber)
        => UniqueConstraintDetector.IsUniqueViolation(errorNumber).Should().BeFalse();

    [Fact]
    public void IsUniqueViolation_Exception_IgnoresNonSqlServerFailures()
    {
        // مزوّد InMemory أو أي فشل آخر لا يحمل SqlException — يجب ألا يُترجم إلى 409
        var exception = new DbUpdateException("فشل حفظ البيانات", new InvalidOperationException("خطأ آخر"));

        UniqueConstraintDetector.IsUniqueViolation(exception).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_Exception_IgnoresDbUpdateExceptionWithoutInnerException()
    {
        UniqueConstraintDetector.IsUniqueViolation(new DbUpdateException("فشل حفظ البيانات"))
            .Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_Exception_IgnoresForeignKeyFailureWithoutSqlException()
    {
        // فشل مفتاح أجنبي لا يُترجم إلى 409 — يبقى 500 كما كان
        var exception = new DbUpdateException(
            "تعذّر الحذف لارتباط السجل بسجلات أخرى", new Exception("FK constraint"));

        UniqueConstraintDetector.IsUniqueViolation(exception).Should().BeFalse();
    }
}
