using FluentAssertions;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

namespace Kharasana.Tests.Tests;

public sealed class ProductionBatchServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context = TestDataSeeder.CreateContext();
    private readonly ProductionBatchService _service;

    public ProductionBatchServiceTests()
    {
        _service = new ProductionBatchService(new UnitOfWork(_context));
    }

    [Fact]
    public async Task GetByOrderId_OtherFactoryOrder_ThrowsForbidden()
    {
        await SeedOrderAsync();

        var act = () => _service.GetByOrderIdAsync(1, UserRole.FactoryEmployee, callerFactoryId: 20);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Create_OtherFactoryOrder_ThrowsForbiddenAndDoesNotPersist()
    {
        await SeedOrderAsync();

        var act = () => _service.CreateAsync(
            new CreateProductionBatchDto { ConcreteTypeId = 1, Quantity = 10 },
            orderId: 1,
            callerRole: UserRole.FactoryEmployee,
            callerFactoryId: 20);

        await act.Should().ThrowAsync<ForbiddenException>();
        (await _context.ProductionBatches.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Admin_CanListAllFactoriesAndCreateBatch()
    {
        await SeedOrderAsync();

        var created = await _service.CreateAsync(
            new CreateProductionBatchDto { ConcreteTypeId = 1, Quantity = 10 },
            orderId: 1,
            callerRole: UserRole.Admin,
            callerFactoryId: null);
        var all = await _service.GetFactoryProductionBatchesAsync(UserRole.Admin, factoryId: null);
        var factoryBatches = await _service.GetFactoryProductionBatchesAsync(UserRole.FactoryEmployee, factoryId: 10);

        created.OrderId.Should().Be(1);
        all.Should().ContainSingle(batch => batch.ProductionBatchId == created.ProductionBatchId);
        factoryBatches.Should().ContainSingle(batch => batch.ProductionBatchId == created.ProductionBatchId);
    }

    [Fact]
    public async Task FactoryEmployee_WithoutFactoryId_CannotListAllBatches()
    {
        var act = () => _service.GetFactoryProductionBatchesAsync(UserRole.FactoryEmployee, factoryId: null);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Create_ConcreteTypeDifferentFromOrder_ThrowsBusinessException()
    {
        await SeedOrderAsync();

        var act = () => _service.CreateAsync(
            new CreateProductionBatchDto { ConcreteTypeId = 99, Quantity = 10 },
            orderId: 1,
            callerRole: UserRole.Admin,
            callerFactoryId: null);

        await act.Should().ThrowAsync<BusinessException>();
        (await _context.ProductionBatches.CountAsync()).Should().Be(0);
    }

    private async Task SeedOrderAsync()
    {
        _context.Factories.Add(TestDataSeeder.CreateFactory(10, "مصنع دفعات الاختبار"));
        _context.Users.Add(TestDataSeeder.CreateUser(2, "عميل دفعات الاختبار", UserRole.Client));
        _context.ConcreteTypes.Add(TestDataSeeder.CreateConcreteType(1, 10, "C30"));
        _context.Orders.Add(TestDataSeeder.CreateOrder(
            orderId: 1,
            clientId: 2,
            factoryId: 10,
            concreteTypeId: 1));
        await _context.SaveChangesAsync();
    }

    public void Dispose() => _context.Dispose();
}