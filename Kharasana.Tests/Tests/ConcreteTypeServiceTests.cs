using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات ConcreteTypeService — التركيز على:
/// • منع إضافة نوع خرسانة في مصنع مؤرشف
/// • منع إضافة/تعديل نوع خرسانة في مصنع موقوف
/// • المصنع الموقوف لا يسمح بالتعديل
/// </summary>
public class ConcreteTypeServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly ConcreteTypeService _service;

    public ConcreteTypeServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _service = new ConcreteTypeService(new UnitOfWork(_context));
    }

    [Fact]
    public async Task Create_ArchivedFactory_ThrowsFactoryArchived()
    {
        // Arrange — مصنع محذوف
        var factory = TestDataSeeder.CreateFactory(100, "محذوفة", isActive: false, isDeleted: true);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 100,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryArchived);
    }

    [Fact]
    public async Task Create_InactiveFactory_ThrowsFactoryInactive()
    {
        // Arrange — مصنع موقوف (غير محذوف)
        var factory = TestDataSeeder.CreateFactory(101, "موقوفة", isActive: false);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(new CreateConcreteTypeDto
        {
            FactoryId = 101,
            Name = "C25",
            Strength = 25,
            UnitPrice = 150m
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryInactive);
    }

    [Fact]
    public async Task Update_InactiveFactory_ThrowsFactoryInactive()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(102, "موقوفة2", isActive: false);
        var ct = TestDataSeeder.CreateConcreteType(200, 102, "C25");
        _context.Factories.Add(factory);
        _context.ConcreteTypes.Add(ct);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.UpdateAsync(200, new UpdateConcreteTypeDto
        {
            Name = "C30",
            Strength = 30,
            UnitPrice = 180m,
            IsActive = true
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryInactive);
    }

    [Fact]
    public async Task GetAll_FiltersByFactory()
    {
        // Arrange — مصنعان، كلٌّ له نوعان
        var f1 = TestDataSeeder.CreateFactory(103, "مصنع_أ");
        var f2 = TestDataSeeder.CreateFactory(104, "مصنع_ب");
        _context.Factories.AddRange(f1, f2);
        _context.ConcreteTypes.AddRange(
            TestDataSeeder.CreateConcreteType(201, 103, "C25_أ"),
            TestDataSeeder.CreateConcreteType(202, 103, "C30_أ"),
            TestDataSeeder.CreateConcreteType(203, 104, "C25_ب"));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync(103)).ToList();

        // Assert — فقط أنواع المصنع 103
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.FactoryId == 103);
    }

    public void Dispose() => _context.Dispose();
}