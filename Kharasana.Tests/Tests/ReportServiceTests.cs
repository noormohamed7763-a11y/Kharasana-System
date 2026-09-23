using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Report;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using FluentAssertions;
using Xunit;

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

    public void Dispose() => _context.Dispose();
}
