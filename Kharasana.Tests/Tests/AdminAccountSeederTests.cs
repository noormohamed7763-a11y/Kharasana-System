using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات AdminAccountSeeder — تهيئة حساب المدير الأول عند الإقلاع.
/// التركيز على:
/// • الإنشاء عند غياب أي مدير
/// • التخطّي الآمن: بلا إعدادات، بكلمة مرور ضعيفة، ببريد محجوز
/// • التكرار بلا أثر: لا يُعاد الإنشاء ولا تُعدّل كلمة مرور قائمة
/// </summary>
public class AdminAccountSeederTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly ServiceProvider _services;

    public AdminAccountSeederTests()
    {
        _context = TestDataSeeder.CreateContext();

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(_context));
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        _services = services.BuildServiceProvider();
    }

    private static IConfiguration Config(string? email, string? password)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSeed:Email"] = email,
                ["AdminSeed:Password"] = password
            })
            .Build();

    private Task SeedAsync(string? email, string? password)
        => AdminAccountSeeder.SeedAsync(_services, Config(email, password), NullLogger.Instance);

    [Fact]
    public async Task Seed_NoAdminExists_CreatesActiveAdmin()
    {
        // Act
        await SeedAsync("admin@kharasana.local", "Admin@12345");

        // Assert — مدير واحد فعّال بلا مصنع، وكلمة المرور مخزَّنة مجزَّأة
        var admin = await _context.Users.AsNoTracking().SingleAsync(u => u.Role == UserRole.Admin);
        admin.Email.Should().Be("admin@kharasana.local");
        admin.IsActive.Should().BeTrue();
        admin.FactoryId.Should().BeNull();
        admin.PasswordHash.Should().NotBeNullOrWhiteSpace();
        admin.PasswordHash.Should().NotContain("Admin@12345");
        BCrypt.Net.BCrypt.Verify("Admin@12345", admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Seed_AdminAlreadyExists_DoesNotCreateSecond()
    {
        // Arrange — مدير قائم بكلمة مرور مُختارة
        await SeedAsync("admin@kharasana.local", "Admin@12345");
        var before = await _context.Users.AsNoTracking().SingleAsync(u => u.Role == UserRole.Admin);

        // Act — إقلاع ثانٍ بإعدادات مختلفة (كلمة مرور جديدة)
        await SeedAsync("other@kharasana.local", "Different@999");

        // Assert — لا مدير ثانٍ، وكلمة المرور القائمة لم تُعدّل
        var all = await _context.Users.AsNoTracking().Where(u => u.Role == UserRole.Admin).ToListAsync();
        all.Should().HaveCount(1);
        all[0].UserId.Should().Be(before.UserId);
        all[0].PasswordHash.Should().Be(before.PasswordHash);
    }

    [Fact]
    public async Task Seed_NoConfiguration_CreatesNothing()
    {
        // Act — بلا أي إعداد (الحالة الافتراضية في appsettings)
        await SeedAsync(null, null);

        // Assert
        (await _context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_WeakPassword_CreatesNothing()
    {
        // Act — كلمة مرور أقصر من الحد الأدنى
        await SeedAsync("admin@kharasana.local", "1234567");

        // Assert — لا يُنشأ حساب مدير ضعيف
        (await _context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_EmailTakenByNonAdmin_DoesNotCreateAdmin()
    {
        // Arrange — البريد محجوز لحساب عميل
        var client = TestDataSeeder.CreateUser(90, "عميل_بالبريد", UserRole.Client,
            email: "admin@kharasana.local");
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        // Act
        await SeedAsync("admin@kharasana.local", "Admin@12345");

        // Assert — لا ترقية تلقائية للعميل إلى مدير، ولا حساب ثانٍ بالبريد نفسه
        (await _context.Users.CountAsync(u => u.Role == UserRole.Admin)).Should().Be(0);
        var stored = await _context.Users.AsNoTracking().SingleAsync(u => u.UserId == 90);
        stored.Role.Should().Be(UserRole.Client);
    }

    [Fact]
    public async Task Seed_InvalidEmail_CreatesNothing()
    {
        // Act
        await SeedAsync("ليس-بريداً", "Admin@12345");

        // Assert
        (await _context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_DatabaseUnreachable_DoesNotThrow()
    {
        // Arrange — وحدة عمل ترمي عند أول استعلام (محاكاة قاعدة بيانات غير متاحة
        // أو غير مُهاجَرة). يجب ألا يُسقط ذلك إقلاع التطبيق.
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork>(_ => new UnreachableUnitOfWork());

        var provider = services.BuildServiceProvider();

        // Act
        var act = () => AdminAccountSeeder.SeedAsync(
            provider, Config("admin@kharasana.local", "Admin@12345"), NullLogger.Instance);

        // Assert — لا استثناء: تُسجَّل المشكلة ويُتابع الإقلاع
        await act.Should().NotThrowAsync();

        provider.Dispose();
    }

    public void Dispose()
    {
        _services.Dispose();
        _context.Dispose();
    }

    /// <summary>وحدة عمل يفشل كل وصول إلى مستودعها — تحاكي قاعدة بيانات غير متاحة.</summary>
    private sealed class UnreachableUnitOfWork : IUnitOfWork
    {
        public IFactoryRepository Factories => throw Unreachable();
        public IUserRepository Users => throw Unreachable();
        public IConcreteTypeRepository ConcreteTypes => throw Unreachable();
        public IOrderRepository Orders => throw Unreachable();

        public Task<int> SaveChangesAsync() => throw Unreachable();

        public void Dispose() { }

        private static InvalidOperationException Unreachable()
            => new("قاعدة البيانات غير متاحة (محاكاة اختبار).");
    }
}
