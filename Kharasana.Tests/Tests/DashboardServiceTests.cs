using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Dashboard;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
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
