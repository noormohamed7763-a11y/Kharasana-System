using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات FactoryService — التركيز على:
/// • GetAll يُرجع المنشآت النشطة فقط
/// • GetArchived يُرجع المنشآت المحذوفة فقط
/// • Delete يضبط IsDeleted = true
/// • Restore يُلغي الحذف
/// • HasAccount يُحدّث بشكل صحيح عبر الاستعلام المجمّع
/// </summary>
public class FactoryServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly FactoryService _service;

    public FactoryServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _service = new FactoryService(new UnitOfWork(_context), new FakeImageStorage());
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeletedFactories()
    {
        // Arrange
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(1, "نشيطة"),
            TestDataSeeder.CreateFactory(2, "محذوفة", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].FactoryId.Should().Be(1);
    }

    [Fact]
    public async Task GetArchived_ReturnsOnlyDeletedFactories()
    {
        // Arrange
        _context.Factories.AddRange(
            TestDataSeeder.CreateFactory(1, "نشيطة2"),
            TestDataSeeder.CreateFactory(2, "محذوفة2", isActive: false, isDeleted: true));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetArchivedAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].FactoryId.Should().Be(2);
    }

    [Fact]
    public async Task Delete_SetsIsDeletedAndClearsLogo()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(3, "لحذف");
        factory.Logo = "/images/factory-logo.png";
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var success = await _service.DeleteAsync(3);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Factories.FindAsync(3);
        updated!.IsDeleted.Should().BeTrue();
        updated.IsActive.Should().BeFalse();
        updated.Logo.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistent_ThrowsNotFound()
    {
        var act = () => _service.DeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage(Messages.FactoryNotFound);
    }

    [Fact]
    public async Task Restore_UnsetsIsDeleted()
    {
        // Arrange — مصنع محذوف
        var factory = TestDataSeeder.CreateFactory(4, "لاستعادته", isActive: false, isDeleted: true);
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();

        // Act
        var success = await _service.RestoreAsync(4);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Factories.FindAsync(4);
        updated!.IsDeleted.Should().BeFalse();
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_MapsHasAccountCorrectly()
    {
        // Arrange — مصنع بموظف + مصنع بدون موظف
        var f1 = TestDataSeeder.CreateFactory(5, "لديها_حساب");
        var f2 = TestDataSeeder.CreateFactory(6, "بدون_حساب");
        var employee = TestDataSeeder.CreateUser(50, "موظف", UserRole.FactoryEmployee, factoryId: 5);
        _context.Factories.AddRange(f1, f2);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetAllAsync()).OrderBy(x => x.FactoryId).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First(x => x.FactoryId == 5).HasAccount.Should().BeTrue();
        result.First(x => x.FactoryId == 6).HasAccount.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}

/// <summary>
/// تنفيذ وهمي لـ IImageStorageService للاختبارات (بدون ملفات فعلية)
/// </summary>
internal class FakeImageStorage : IImageStorageService
{
    public Task<string> SaveImageAsync(System.IO.Stream stream, string fileName, long fileSize, string subFolder)
        => Task.FromResult($"/images/{subFolder}/{fileName}");

    public void DeleteImage(string? imagePath) { }
}