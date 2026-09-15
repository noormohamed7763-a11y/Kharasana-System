using Kharasana.Application.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات PaginationParams — معاملات الترقيم المشتركة في فلاتر القوائم.
/// قنّن القيم لتجنّب أن تُسبب مدخلات غير صالحة استثناءات خطأ 500
/// (مثل قيمة PageNumber سالبة تصل إلى Skip وتُرمي ArgumentOutOfRangeException).
/// </summary>
public class PaginationParamsTests
{
    // ─────────────────────────────────────────────
    // القيم الافتراضية
    // ─────────────────────────────────────────────

    [Fact]
    public void Defaults_AreReasonable()
    {
        var sut = new PaginationParams();

        sut.PageNumber.Should().Be(1);
        sut.PageSize.Should().Be(20);
        sut.Search.Should().BeNull();
        sut.Status.Should().BeNull();
        sut.FactoryId.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    // القيود على PageSize
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(-5, 1)]     // أقل من الحد → يُرفع إلى 1
    [InlineData(0, 1)]      // صفر → 1
    [InlineData(50, 50)]    // قيمة صحيحة تُحتفظ بها
    [InlineData(100, 100)]  // الحد الأقصى يُحتفظ به
    [InlineData(500, 100)]  // تجاوز الحد → يُخفض إلى 100
    public void PageSize_IsClamped(int input, int expected)
    {
        var sut = new PaginationParams { PageSize = input };

        sut.PageSize.Should().Be(expected);
    }

    // ─────────────────────────────────────────────
    // القيود على PageNumber — الإصلاح الجديد
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(-3, 1)]     // سالب → 1 (كان يُمرّر لـ Skip سابقاً فيرمي 500)
    [InlineData(0, 1)]      // صفر → 1
    [InlineData(1, 1)]      // القيمة الافتراضية الذكرى
    [InlineData(7, 7)]      // قيمة صحيحة تُحتفظ بها
    [InlineData(1000, 1000)] // صفحات كثيرة مسموحة (لا حد أعلى)
    public void PageNumber_IsClampedToMinimumOne(int input, int expected)
    {
        var sut = new PaginationParams { PageNumber = input };

        sut.PageNumber.Should().Be(expected);
    }

    // ─────────────────────────────────────────────
    // الفلاتر الاختيارية
    // ─────────────────────────────────────────────

    [Fact]
    public void Filters_ArePropagated()
    {
        var sut = new PaginationParams
        {
            Search = "الخرسانه",
            Status = OrderStatus.OnTheWay,
            FactoryId = 4
        };

        sut.Search.Should().Be("الخرسانه");
        sut.Status.Should().Be(OrderStatus.OnTheWay);
        sut.FactoryId.Should().Be(4);
    }
}