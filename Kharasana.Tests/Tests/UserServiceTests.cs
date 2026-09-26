using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Services;
using Kharasana.Domain.Enums;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات UserService — التركيز على:
/// • تفعيل/إيقاف حساب السائق (ToggleDriverActiveAsync)
/// • تأكيد الصلاحيات حسب الدور والمصنع
/// </summary>
public class UserServiceTests : IDisposable
{
    private readonly KharasanaDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _context = TestDataSeeder.CreateContext();
        _unitOfWork = new UnitOfWork(_context);

        var passwordHasher = new PasswordHasher();
        _userService = new UserService(_unitOfWork, passwordHasher);
    }

    // ─────────────────────────────────────────────
    // ToggleDriverActiveAsync
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_ActiveDriver_BecomesInactive()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var driver = TestDataSeeder.CreateDriver(
            200, "سائق_猢ّل", 1, isActive: true, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — المدير يعطل السائق النشط
        var newActiveState = await _userService.ToggleDriverActiveAsync(
            200, UserRole.Admin, callerFactoryId: null);

        // Assert
        newActiveState.Should().BeFalse();
        var updated = await _context.Users.FindAsync(200);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_InactiveDriver_BecomesActive()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var driver = TestDataSeeder.CreateDriver(
            201, "سائق_معطّل", 1, isActive: false, status: DriverStatus.Offline);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — المدير يُفعّل السائق المعطّل
        var newActiveState = await _userService.ToggleDriverActiveAsync(
            201, UserRole.Admin, callerFactoryId: null);

        // Assert
        newActiveState.Should().BeTrue();
        var updated = await _context.Users.FindAsync(201);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_NonDriverUser_ThrowsBusinessException()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var client = TestDataSeeder.CreateUser(
            300, "عميل_اختبار", UserRole.Client, phone: "0500000300");
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _userService.ToggleDriverActiveAsync(
            300, UserRole.Admin, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("المستخدم المحدد ليس سائقًا.");
    }

    [Fact]
    public async Task ToggleActive_FactoryEmployee_OtherFactory_ThrowsForbiddenException()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var driver = TestDataSeeder.CreateDriver(
            202, "سائق_مصنع1", 1, isActive: true, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — موظف مصنع 2 يحاول تعطيل سائق مصنع 1
        var act = () => _userService.ToggleDriverActiveAsync(
            202, UserRole.FactoryEmployee, callerFactoryId: 999);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ToggleActive_MissingFactoryId_ThrowsForbiddenException()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var driver = TestDataSeeder.CreateDriver(
            203, "سائق_بدون_مصنع", 1, isActive: true, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — موظف مصنع بلا factoryId
        var act = () => _userService.ToggleDriverActiveAsync(
            203, UserRole.FactoryEmployee, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ToggleActive_FactoryEmployee_SameFactory_Succeeds()
    {
        // Arrange
        await SeedFactoryAndDriverAsync();
        var driver = TestDataSeeder.CreateDriver(
            204, "سائق_مصنع1_مفعل", 1, isActive: true, status: DriverStatus.Available);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act — موظف المصنع 1 يعطل سائق مصنع 1
        var newActiveState = await _userService.ToggleDriverActiveAsync(
            204, UserRole.FactoryEmployee, callerFactoryId: 1);

        // Assert
        newActiveState.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActive_NonExistentDriver_ThrowsNotFoundException()
    {
        // Act
        var act = () => _userService.ToggleDriverActiveAsync(
            9999, UserRole.Admin, callerFactoryId: null);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─────────────────────────────────────────────
    // CreateAsync — عقد (الدور ↔ المصنع) الذي يبنيه عميل الـ Web على القيم الرقمية
    // ─────────────────────────────────────────────

    /// <summary>
    /// دور Client لا يتطلّب مصنعاً فيُقبل بلا FactoryId.
    /// هذا هو العقد الذي ينكسر إذا أرسل عميل الـ Web القيمة 3 (Driver) بدل 4 (Client).
    /// </summary>
    [Fact]
    public async Task CreateAsync_ClientRoleWithoutFactory_Succeeds()
    {
        var dto = new CreateUserDto
        {
            FullName = "عميل اختبار",
            Email = "client-role-contract@test.local",
            Password = TestDataSeeder.TestPassword,
            Phone = "771110001",
            Role = UserRole.Client
        };

        var created = await _userService.CreateAsync(dto);

        created.Role.Should().Be(UserRole.Client.ToString());
        created.FactoryId.Should().BeNull();
    }

    /// <summary>
    /// دور Driver بلا مصنع مرفوض — وبهذا الخطأ تحديداً كان ينتهي مسار المدير
    /// عند إرسال Role=3 مكان Role=4 من عميل الـ Web.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DriverRoleWithoutFactory_ThrowsBusinessException()
    {
        var dto = new CreateUserDto
        {
            FullName = "سائق اختبار",
            Email = "driver-role-contract@test.local",
            Password = TestDataSeeder.TestPassword,
            Phone = "771110002",
            Role = UserRole.Driver
        };

        var act = async () => await _userService.CreateAsync(dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.FactoryRequiredForDriver);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private async Task SeedFactoryAndDriverAsync()
    {
        var factory = TestDataSeeder.CreateFactory(1, "مصنع_اختبار");
        _context.Factories.Add(factory);
        await _context.SaveChangesAsync();
    }

    public void Dispose() => _context.Dispose();
}
