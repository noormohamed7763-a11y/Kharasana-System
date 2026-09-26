using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات DashboardService — التركيز على:
/// /// GetAdminDashboardAsync — عدّادات صحيحة
/// GetFactoryDashboardAsync — فلترة بحسب المصنع وحسب الحالة
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _service = new DashboardService(new UnitOfWork(_context));
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        var f2 = TestDataSeeder.CreateFactory(2, "مصنع ب");
        var admin = TestDataSeeder.CreateUser(10, "مدير", UserRole.Admin);
        var client1 = TestDataSeeder.CreateUser(11, "عميل أ", UserRole.Client);
        var client2 = TestDataSeeder.CreateUser(12, "عميل ب", UserRole.Client);
        var employee = TestDataSeeder.CreateUser(13, "موظف", UserRole.FactoryEmployee, factoryId: 1);
        var driver = TestDataSeeder.CreateDriver(14, "سائق", factoryId: 1);
        _context.Factories.AddRange(f1, f2);
        _context.Users.AddRange(admin, client1, client2, employee, driver);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAdminDashboardAsync();

        // Assert
        result.TotalFactories.Should().Be(2);
        result.TotalClients.Should().Be(2);
        result.TotalEmployees.Should().Be(1);
        result.TotalDrivers.Should().Be(1);
        result.TotalOrders.Should().Be(0);
    }

    [Fact]
    public async Task GetFactoryDashboardAsync_FiltersByFactory()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        var f2 = TestDataSeeder.CreateFactory(2, "مصنع ب");
        _context.Factories.AddRange(f1, f2);

        var o1 = TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.New);
        var o2 = TestDataSeeder.CreateOrder(2, 101, 1, 1, status: OrderStatus.Pending);
        var o3 = TestDataSeeder.CreateOrder(3, 102, 2, 1, status: OrderStatus.Approved);
        _context.Orders.AddRange(o1, o2, o3);
        await _context.SaveChangesAsync();

        // Act — مصنع 1
        var result1 = await _service.GetFactoryDashboardAsync(1);
        // Act — مصنع 2
        var result2 = await _service.GetFactoryDashboardAsync(2);

        // Assert — مصنع 1: طلبان أُنشئا اليوم (New + Pending)
        result1.TodayOrders.Should().Be(2);
        result1.PendingOrders.Should().Be(1);
        result1.ApprovedOrders.Should().Be(0);

        // Assert — مصنع 2: طلب واحد أُنشئ اليوم (Approved)
        result2.TodayOrders.Should().Be(1);
        result2.PendingOrders.Should().Be(0);
        result2.ApprovedOrders.Should().Be(1);
    }

    [Fact]
    public async Task GetFactoryDashboardAsync_TodayOrders_ExcludesOlderOrders()
    {
        // Arrange — ثلاثة طلبات في المصنع نفسه: اليوم، أمس، وقبل ثلاثة أيام
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        _context.Factories.Add(f1);

        var today = TestDataSeeder.CreateOrder(1, 100, 1, 1);
        var yesterday = TestDataSeeder.CreateOrder(2, 101, 1, 1);
        yesterday.CreatedAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(12);
        var older = TestDataSeeder.CreateOrder(3, 102, 1, 1);
        older.CreatedAt = DateTime.UtcNow.Date.AddDays(-3);

        _context.Orders.AddRange(today, yesterday, older);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFactoryDashboardAsync(1);

        // Assert — «طلبات اليوم» تُفلتر بالزمن (واحد فقط)، بينما PendingOrders تُعدّ بالحالة (الثلاثة)
        result.TodayOrders.Should().Be(1);
        result.PendingOrders.Should().Be(3);
    }

    /// <summary>
    /// حدود اليوم: منتصف الليل بتوقيت اليمن (UTC+3) لا منتصف الليل UTC.
    ///
    /// <para>كان <c>DateTime.UtcNow.Date</c> يُسقط أول ثلاث ساعات من اليوم المحلي في
    /// «أمس»، فتظهر «طلبات اليوم» و«تم التسليم اليوم» ناقصة بلا أي خطأ ظاهر. هذا
    /// الاختبار يثبّت الثوابت الأربعة التي يقوم عليها النطاق — وهو حتمي بخلاف اختبار
    /// الخدمة أدناه الذي يعتمد على ساعة الجدار.</para>
    /// </summary>
    [Fact]
    public void YemenTime_TodayUtcRange_IsTheLocalDayExpressedInUtc()
    {
        var (start, end) = YemenTime.TodayUtcRange();

        (end - start).Should().Be(TimeSpan.FromDays(1), "اليوم نطاق نصف مفتوح طوله 24 ساعة");

        start.Should().Be(
            YemenTime.Today - YemenTime.Offset,
            "البداية هي منتصف الليل بتوقيت اليمن معبَّراً عنه بـ UTC");

        // منتصف الليل المحلي يقع في اليوم الميلادي السابق بتوقيت UTC — وهذه الساعات
        // الثلاث الأولى هي بالضبط ما كان DateTime.UtcNow.Date يُسقطه في «أمس».
        start.Date.Should().Be(
            YemenTime.Today.AddDays(-1),
            "منتصف الليل المحلي يقع مساء اليوم السابق بتوقيت UTC");

        // وسط النهار المحلي لا بد أن يكون داخل النطاق، وكذلك اللحظة الحالية
        DateTime.UtcNow.Should().BeOnOrAfter(start).And.BeBefore(end);

        // الطرفان منتصف الليل المحلي بالضبط، لا منتصف ليل UTC:
        (start + YemenTime.Offset).TimeOfDay.Should().Be(
            TimeSpan.Zero, "بداية النطاق هي منتصف الليل بتوقيت اليمن");
        (end + YemenTime.Offset).TimeOfDay.Should().Be(
            TimeSpan.Zero, "ونهايته منتصف الليل التالي بالتوقيت نفسه");

        // وهذا الفرق هو العطل نفسه: نافذة العطل القديمة كانت تبدأ عند
        // DateTime.UtcNow.Date (00:00 UTC = الثالثة فجراً بتوقيت اليمن)، فتُسقط
        // أول ثلاث ساعات من اليوم المحلي في «أمس» بلا أي خطأ ظاهر. البداية الصحيحة
        // تقع دائماً في الساعة 21:00 UTC من اليوم الميلادي السابق.
        start.TimeOfDay.Should().NotBe(
            TimeSpan.Zero, "بداية اليوم المحلي ليست منتصف ليل UTC ما دامت الإزاحة غير صفرية");
        start.Should().NotBe(
            DateTime.UtcNow.Date, "بداية النافذة القديمة هي DateTime.UtcNow.Date — وهي مختلفة دائماً");
    }

    /// <summary>
    /// طلب أُنشئ في الساعة الأولى من اليوم المحلي يُحتسب ضمن «طلبات اليوم».
    ///
    /// <para><b>حدّ هذا الاختبار:</b> يعتمد على ساعة الجدار. يُميّز الإصلاح عن العطل
    /// في 21 ساعة من 24 (حين تكون الساعة الحالية UTC بين 00:00 و21:00)؛ وفي الساعات
    /// الثلاث الأخيرة من اليوم UTC يقع الطلب داخل اليومين معاً فيمرّ على الحالتين.
    /// لا يفشل زوراً في أي وقت — الحارس الحتمي هو الاختبار أعلاه.</para>
    /// </summary>
    [Fact]
    public async Task GetFactoryDashboardAsync_TodayOrders_UsesYemenDayBoundary()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        _context.Factories.Add(f1);

        var localMidnightUtc = YemenTime.TodayUtcRange().StartUtc;

        // 01:00 بتوقيت اليمن = 22:00 UTC من اليوم السابق — «اليوم» محلياً
        var afterLocalMidnight = TestDataSeeder.CreateOrder(1, 100, 1, 1);
        afterLocalMidnight.CreatedAt = localMidnightUtc.AddHours(1);

        // دقيقة واحدة قبل منتصف الليل المحلي — «أمس» محلياً
        var beforeLocalMidnight = TestDataSeeder.CreateOrder(2, 101, 1, 1);
        beforeLocalMidnight.CreatedAt = localMidnightUtc.AddMinutes(-1);

        _context.Orders.AddRange(afterLocalMidnight, beforeLocalMidnight);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFactoryDashboardAsync(1);

        // Assert
        result.TodayOrders.Should().Be(
            1, "حدّ اليوم هو منتصف الليل بتوقيت اليمن: الأول داخل والثاني خارج");
    }

    /// <summary>
    /// نطاقا العدّ مختلفان: «طلبات اليوم» زمني، وعدّادات الحالات تراكمية.
    /// هذا الاختلاف هو أصل العلّة في لوحة المصنع — كان العرض يجمعهما فيُحصي
    /// كل طلب أُنشئ اليوم وما زال مفتوحاً مرّتين.
    /// </summary>
    [Fact]
    public async Task GetFactoryDashboardAsync_StatusCountsAreNotScopedToToday()
    {
        // Arrange — طلب قديم ما زال قيد الانتظار
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        _context.Factories.Add(f1);

        var old = TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.Pending);
        old.CreatedAt = YemenTime.TodayUtcRange().StartUtc.AddDays(-30);
        _context.Orders.Add(old);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFactoryDashboardAsync(1);

        // Assert
        result.TodayOrders.Should().Be(0, "أُنشئ قبل ثلاثين يوماً");
        result.PendingOrders.Should().Be(1, "تراكمي: ما زال في حالة الانتظار الآن");
    }

    [Fact]
    public async Task GetFactoryDashboardAsync_CountsDeliveredToday()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        _context.Factories.Add(f1);

        var o1 = TestDataSeeder.CreateOrder(1, 100, 1, 1, quantity: 10, status: OrderStatus.Delivered, unitPrice: 150m);
        o1.DeliveredAt = DateTime.UtcNow;
        var o2 = TestDataSeeder.CreateOrder(2, 101, 1, 1, quantity: 20, status: OrderStatus.Delivered, unitPrice: 200m);
        o2.DeliveredAt = DateTime.UtcNow.AddDays(-1); // أمس
        _context.Orders.AddRange(o1, o2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFactoryDashboardAsync(1);

        // Assert — فقط التسليم اليومي
        result.DeliveredToday.Should().Be(1);
    }

    [Fact]
    public async Task GetFactoryDashboardAsync_CountsAvailableDrivers()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        var availableDriver = TestDataSeeder.CreateDriver(100, "سائق متاح", 1, isActive: true, status: DriverStatus.Available);
        var busyDriver = TestDataSeeder.CreateDriver(101, "سائق مشغول", 1, isActive: true, status: DriverStatus.Busy);
        var offlineDriver = TestDataSeeder.CreateDriver(102, "سائق غير متصل", 1, isActive: true, status: DriverStatus.Offline);
        _context.Factories.Add(f1);
        _context.Users.AddRange(availableDriver, busyDriver, offlineDriver);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFactoryDashboardAsync(1);

        // Assert — فقط السائق المتاح
        result.AvailableDrivers.Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
