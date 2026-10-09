using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات ReportService — التركيز على:
/// GetReportSummaryAsync — عدّادات صحيحة حسب الحالة ونوع الخرسانة
/// </summary>
public class ReportServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _service = new ReportService(new UnitOfWork(_context));
    }

    [Fact]
    public async Task GetReportSummaryAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        var f2 = TestDataSeeder.CreateFactory(2, "مصنع ب");
        var ct1 = TestDataSeeder.CreateConcreteType(1, 1, "C25");
        var ct2 = TestDataSeeder.CreateConcreteType(2, 2, "C30");
        _context.Factories.AddRange(f1, f2);
        _context.ConcreteTypes.AddRange(ct1, ct2);

        var o1 = TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.New);
        var o2 = TestDataSeeder.CreateOrder(2, 101, 1, 1, status: OrderStatus.Pending);
        var o3 = TestDataSeeder.CreateOrder(3, 102, 1, 1, status: OrderStatus.Approved);
        var o4 = TestDataSeeder.CreateOrder(4, 103, 2, 2, status: OrderStatus.New);
        var o5 = TestDataSeeder.CreateOrder(5, 104, 2, 2, status: OrderStatus.Rejected);
        _context.Orders.AddRange(o1, o2, o3, o4, o5);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReportSummaryAsync();

        // Assert — كل المصانع
        result.TotalOrders.Should().Be(5);
        result.OrdersByStatus.Should().HaveCount(4); // New, Pending, Approved, Rejected
        result.OrdersByConcreteType.Should().HaveCount(2); // C25 + C30
    }

    [Fact]
    public async Task GetReportSummaryAsync_FiltersByFactory()
    {
        // Arrange
        var f1 = TestDataSeeder.CreateFactory(1, "مصنع أ");
        var f2 = TestDataSeeder.CreateFactory(2, "مصنع ب");
        var ct1 = TestDataSeeder.CreateConcreteType(1, 1, "C25");
        _context.Factories.AddRange(f1, f2);
        _context.ConcreteTypes.Add(ct1);

        _context.Orders.AddRange(
            TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.New),
            TestDataSeeder.CreateOrder(2, 101, 1, 1, status: OrderStatus.Pending),
            TestDataSeeder.CreateOrder(3, 102, 2, 1, status: OrderStatus.Approved)
        );
        await _context.SaveChangesAsync();

        // Act — مصنع 1 فقط
        var result = await _service.GetReportSummaryAsync(factoryId: 1);

        // Assert
        result.TotalOrders.Should().Be(2);
        result.OrdersByStatus.Should().HaveCount(2); // New + Pending
        result.OrdersByConcreteType.Should().HaveCount(1); // C25
    }

    [Fact]
    public async Task GetReportSummaryAsync_EmptyDatabase_ReturnsZeros()
    {
        // Act
        var result = await _service.GetReportSummaryAsync();

        // Assert
        result.TotalOrders.Should().Be(0);
        result.OrdersByStatus.Should().BeEmpty();
        result.OrdersByConcreteType.Should().BeEmpty();
    }

    /// <summary>
    /// طلب يعود إلى نوع خرسانة محذوف ناعمًا يجب ألا يسقط من التقرير.
    /// كان الانضمام إلى ConcreteTypes يحمل فلتر (!IsDeleted) فيتحول إلى INNER JOIN
    /// يُسقط الصف، ويهبط العدّ الكلي سرًّا. الآن يُتجاوز الفلتر على أنواع الخرسانة فقط
    /// فيبقى الطلب محسوبًا ومُعرَّفًا بمعرّف نوعه واسمه.
    /// ملاحظة: هذا اختبار InMemory لمنطق الاستعلام — إنفاذ الفهرس الفريد المُرشَّح
    /// على SQL Server لم يُنفَّذ (انظر SQL_SERVER_INTEGRATION_TESTING.md).
    /// </summary>
    [Fact]
    public async Task GetReportSummaryAsync_OrderOfSoftDeletedConcreteType_IsStillCounted()
    {
        // Arrange — نوع نشط ونوع محذوف في المصنع نفسه، ولكلٍّ طلباته
        _context.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع تقرير"));
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(1, 1, "C25"),
            TestDataSeeder.CreateConcreteType(2, 1, "C30", isActive: false, isDeleted: true));

        _context.Orders.AddRange(
            TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.New),
            TestDataSeeder.CreateOrder(2, 101, 1, 2, quantity: 20, status: OrderStatus.Approved));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReportSummaryAsync(factoryId: 1);

        // Assert — الطلبان محسوبان، ونوع الخرسانة المحذوف ما زال مُعرَّفًا باسمه
        result.TotalOrders.Should().Be(2);
        result.OrdersByConcreteType.Should().HaveCount(2);

        var deletedTypeEntry = result.OrdersByConcreteType.Single(x => x.ConcreteTypeId == 2);
        deletedTypeEntry.ConcreteTypeName.Should().Be("C30");
        deletedTypeEntry.Count.Should().Be(1);
        deletedTypeEntry.TotalQuantity.Should().Be(20);
    }

    /// <summary>
    /// حذف نوع خرسانة فعليًا لا يُغيّر أرقام التقرير: العدّ قبل الحذف وبعده متطابقان.
    /// </summary>
    [Fact]
    public async Task GetReportSummaryAsync_CountsUnchangedAfterSoftDeletingConcreteType()
    {
        // Arrange
        _context.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع تقرير 2"));
        _context.Users.Add(TestDataSeeder.CreateUser(100, "عميل تقرير", UserRole.Client, phone: "050000100"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(1, 1, "C25"));
        _context.Orders.AddRange(
            TestDataSeeder.CreateOrder(1, 100, 1, 1, status: OrderStatus.New),
            TestDataSeeder.CreateOrder(2, 100, 1, 1, quantity: 30, status: OrderStatus.Approved));
        await _context.SaveChangesAsync();

        var before = await _service.GetReportSummaryAsync(factoryId: 1);

        // Act — حذف ناعم لنوع الخرسانة يدوياً
        var ct = await _context.ConcreteTypes.FindAsync(1);
        if (ct != null) { ct.IsDeleted = true; await _context.SaveChangesAsync(); }

        var after = await _service.GetReportSummaryAsync(factoryId: 1);

        // Assert
        after.TotalOrders.Should().Be(before.TotalOrders).And.Be(2);
        after.OrdersByConcreteType.Should().HaveCount(1);
        after.OrdersByConcreteType.Single().ConcreteTypeId.Should().Be(1);
        after.OrdersByConcreteType.Single().TotalQuantity.Should().Be(80m);
    }

    /// <summary>
    /// الطلبات المحذوفة ناعمًا تبقى مستبعدة من التقرير: تجاوز الفلتر مقصور على أنواع
    /// الخرسانة، ويُعاد تطبيق فلتر الطلبات يدويًا بعد <c>IgnoreQueryFilters</c>
    /// (لأنه يلغي فلاتر كل الكيانات في الاستعلام لا مجموعة واحدة).
    /// </summary>
    [Fact]
    public async Task GetReportSummaryAsync_SoftDeletedOrder_IsStillExcluded()
    {
        // Arrange — طلب سليم وطلب محذوف ناعمًا على النوع نفسه
        _context.Factories.Add(TestDataSeeder.CreateFactory(1, "مصنع تقرير 3"));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(1, 1, "C25"));

        var deletedOrder = TestDataSeeder.CreateOrder(2, 101, 1, 1, quantity: 40, status: OrderStatus.New);
        deletedOrder.IsDeleted = true;

        _context.Orders.AddRange(
            TestDataSeeder.CreateOrder(1, 100, 1, 1, quantity: 10, status: OrderStatus.New),
            deletedOrder);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetReportSummaryAsync(factoryId: 1);

        // Assert — الطلب المحذوف غير محسوب لا في المجموع الكلي ولا في عدّ النوع
        result.TotalOrders.Should().Be(1);
        result.OrdersByConcreteType.Should().HaveCount(1);
        result.OrdersByConcreteType.Single().Count.Should().Be(1);
        result.OrdersByConcreteType.Single().TotalQuantity.Should().Be(10m);
    }

    public void Dispose() => _context.Dispose();
}


