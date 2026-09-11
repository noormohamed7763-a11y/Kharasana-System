using FluentAssertions;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Services;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.Extensions.Options;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات التحقق من صلاحيات الدخول — التركيز على:
/// • المصنع المحذوف (مؤرشف) يمنع دخول موظف/سائق
/// • المصنع الموقوف يسمح بالدخول مع تحذير
/// • السائق الموقوف يُمنع بالرسالة الخاصة
/// • حماية الحساب من التخمين العنيف (قفل بعد 5 محاولات)
/// • إعادة تعيين العدّاد بعد نجاح تسجيل الدخول
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _context = TestDataSeeder.CreateContext();

        var unitOfWork = new UnitOfWork(_context);
        var passwordHasher = new PasswordHasher();
        var tokenService = new TokenService(Options.Create(new JwtSettings
        {
            Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", // 64 bytes ≥ 256-bit HS256
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        }));

        _authService = new AuthService(unitOfWork, passwordHasher, tokenService);
    }

    // ─────────────────────────────────────────────
    // ①  مصنع مؤرشف (IsDeleted) يمنع الدخول
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_ArchivedFactory_Employee_ThrowsFactoryArchived()
    {
        // Arrange — مصنع محذوف + موظف مرتبط به
        var factory = TestDataSeeder.CreateFactory(1, "محذوفة", isActive: false, isDeleted: true);
        var employee = TestDataSeeder.CreateUser(10, "موظف محذوف", UserRole.FactoryEmployee, factoryId: 1);
        _context.Factories.Add(factory);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = employee.Email!,
            Password = "Test@1234"
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryArchived);
    }

    [Fact]
    public async Task Login_ArchivedFactory_Driver_ThrowsFactoryArchived()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(1, "محذوفة2", isActive: false, isDeleted: true);
        var driver = TestDataSeeder.CreateDriver(10, "سائق محذوف", factoryId: 1);
        _context.Factories.Add(factory);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = driver.Phone!,
            Password = "Test@1234"
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryArchived);
    }

    // ─────────────────────────────────────────────
    // ②  مصنع موقوف (IsActive=false) — الدخول مع تحذير
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_InactiveFactory_Employee_CanLoginWithNotification()
    {
        // Arrange — مصنع موقوف + موظف نشط
        var factory = TestDataSeeder.CreateFactory(2, "موقوفة", isActive: false, isDeleted: false);
        var employee = TestDataSeeder.CreateUser(20, "موظف موقوف", UserRole.FactoryEmployee, factoryId: 2);
        _context.Factories.Add(factory);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = employee.Email!,
            Password = "Test@1234"
        });

        // Assert — نجاح الدخول + تحذير موجود + IsActive = false
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Notification.Should().Be(Messages.FactoryInactiveLogin);
        result.Data.FactoryIsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ActiveFactory_Employee_HasNoNotification()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(3, "نشيطة", isActive: true);
        var employee = TestDataSeeder.CreateUser(30, "موظف نشط", UserRole.FactoryEmployee, factoryId: 3);
        _context.Factories.Add(factory);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = employee.Email!,
            Password = "Test@1234"
        });

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Notification.Should().BeNull();
        result.Data.FactoryIsActive.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    // ③  سائق موقوف — رسالة خاصة DriverDeactivated
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_DeactivatedDriver_ThrowsDriverDeactivated()
    {
        // Arrange
        var driver = TestDataSeeder.CreateDriver(40, "سائق موقوف", factoryId: 1, isActive: false);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = driver.Phone!,
            Password = "Test@1234"
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.DriverDeactivated);
    }

    [Fact]
    public async Task Login_DeactivatedClient_ThrowsUserInactive()
    {
        // Arrange
        var client = TestDataSeeder.CreateUser(50, "عميل موقوف", UserRole.Client, isActive: false, phone: "770000050");
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = client.Phone!,
            Password = "Test@1234"
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.UserInactive);
    }

    // ─────────────────────────────────────────────
    // ④  حماية الحساب من التخمين العنيف
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_WrongPassword_IncrementsFailedAttempts()
    {
        // Arrange
        var user = TestDataSeeder.CreateUser(60, "حساب عادي", UserRole.Client, phone: "770000060");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act — محاولة خاطئة واحدة
        await Assert.ThrowsAsync<BusinessException>(() => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = user.Phone!,
            Password = " WRONG "
        }));

        // Assert
        var updated = await _context.Users.FindAsync(60);
        updated!.FailedLoginAttempts.Should().Be(1);
        updated.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Login_FailedAttempts_ReachesLockout()
    {
        // Arrange — مستخدم مع 4 محاولات فاشلة سابقة
        var user = TestDataSeeder.CreateUser(61, "濒临 قفل", UserRole.Client, phone: "770000061");
        user.FailedLoginAttempts = 4;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act — المحاولة الخامسة الفاشلة تُفعّل القفل
        await Assert.ThrowsAsync<BusinessException>(() => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = user.Phone!,
            Password = " WRONG "
        }));

        // Assert
        var updated = await _context.Users.FindAsync(61);
        updated!.FailedLoginAttempts.Should().Be(5);
        updated.LockoutEnd.Should().NotBeNull();
        updated.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public async Task Login_LockedAccount_ThrowsAccountLocked()
    {
        // Arrange — حساب مقفل
        var user = TestDataSeeder.CreateUser(62, "حساب مقفل", UserRole.Client, phone: "770000062");
        user.FailedLoginAttempts = 5;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10); // مقفل لمدة 10 دقائق من الآن
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = user.Phone!,
            Password = "Test@1234" // كلمة المرور صحيحة لكن الحساب مقفل
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.AccountLocked);
    }

    // ─────────────────────────────────────────────
    // ⑤  نجاح تسجيل الدخول — إعادة تعيين العدّاد
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_Success_ResetsFailedAttempts()
    {
        // Arrange — مستخدم مع محاولات فاشلة سابقة
        var user = TestDataSeeder.CreateUser(70, "يعود لل Leben", UserRole.Client, phone: "770000070");
        user.FailedLoginAttempts = 3;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = user.Phone!,
            Password = "Test@1234"
        });

        // Assert
        var updated = await _context.Users.FindAsync(70);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockoutEnd.Should().BeNull();
        updated.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WrongPhone_ThrowsInvalidCredentials()
    {
        // Act
        var act = () => _authService.LoginAsync(new LoginRequestDto
        {
            EmailOrPhone = "050000000",
            Password = "any"
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    public void Dispose() => _context.Dispose();
}