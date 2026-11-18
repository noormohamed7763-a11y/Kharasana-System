using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Services;
using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;
using Kharasana.Infrastructure.Authentication;
using Kharasana.Infrastructure.Persistence;
using Kharasana.Tests.TestData;
using Microsoft.EntityFrameworkCore;

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
    // CreateAsync — الحد الأدنى لطول كلمة المرور (كان غائباً تماماً)
    // ─────────────────────────────────────────────

    /// <summary>
    /// الخدمة هي الحارس الفعلي لكل مسارات إنشاء المستخدمين: كانت تُخزَّن كلمة مرور
    /// من محرف واحد بلا اعتراض لأن CreateUserDto بلا DataAnnotations وكان
    /// CreateUserDtoValidator غير مُشغَّل. الرقم من PasswordPolicy.MinimumLength.
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("1234567")]  // محرف أقل من الحد الأدنى — كان مقبولاً قبل التوحيد
    [InlineData("")]
    public async Task CreateAsync_PasswordBelowMinimum_ThrowsBusinessException(string password)
    {
        var dto = new CreateUserDto
        {
            FullName = "مستخدم بكلمة مرور قصيرة",
            Email = $"short-password-{password.Length}@test.local",
            Password = password,
            Phone = "771110003",
            Role = UserRole.Client
        };

        var act = async () => await _userService.CreateAsync(dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.PasswordMinLength);

        // لا يُنشأ أي حساب بكلمة مرور مرفوضة
        (await _context.Users.CountAsync(u => u.Email == dto.Email)).Should().Be(0);
    }

    /// <summary>الحد الأدنى نفسه مقبول — الفحص ليس متشدّداً أكثر من السياسة.</summary>
    [Fact]
    public async Task CreateAsync_PasswordAtMinimum_Succeeds()
    {
        var dto = new CreateUserDto
        {
            FullName = "مستخدم بالحد الأدنى",
            Email = "at-minimum@test.local",
            Password = new string('a', PasswordPolicy.MinimumLength),
            Phone = "771110004",
            Role = UserRole.Client
        };

        var created = await _userService.CreateAsync(dto);

        created.Role.Should().Be(UserRole.Client.ToString());
    }

    // ─────────────────────────────────────────────
    // UpdateAsync — البريد الإلكتروني
    //
    // كان UpdateUserDto بلا حقل Email بينما مسارات الويب ترسله، فيُهمَل صامتاً:
    // تُحفظ بقية الحقول وتظهر رسالة نجاح ولا يتغيّر البريد.
    // ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NewEmail_IsPersisted()
    {
        var user = TestDataSeeder.CreateUser(300, "عميل_بريد", UserRole.Client, email: "old@test.local");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            FullName = "عميل_بريد",
            Role = UserRole.Client,
            Phone = user.Phone,
            Email = "new@test.local"
        };

        await _userService.UpdateAsync(300, dto);

        var updated = await _context.Users.FindAsync(300);
        updated!.Email.Should().Be("new@test.local", "البريد المُرسَل من نموذج التعديل يجب أن يُحفظ");
    }

    [Fact]
    public async Task UpdateAsync_EmailTakenByAnotherUser_ThrowsConflict()
    {
        var mine = TestDataSeeder.CreateUser(301, "عميل_أ", UserRole.Client, email: "mine@test.local");
        var other = TestDataSeeder.CreateUser(302, "عميل_ب", UserRole.Client, email: "taken@test.local");
        _context.Users.AddRange(mine, other);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            FullName = "عميل_أ",
            Role = UserRole.Client,
            Phone = mine.Phone,
            Email = "taken@test.local"
        };

        var act = async () => await _userService.UpdateAsync(301, dto);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.EmailAlreadyExists);

        // لا تغيير جزئي عند الفشل
        (await _context.Users.FindAsync(301))!.Email.Should().Be("mine@test.local");
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnEmail_DoesNotConflictWithItself()
    {
        // حارس على استثناء المستخدم نفسه (excludeUserId): بدونه يفشل حفظ أي تعديل
        // لمستخدم يملك بريداً لأنه يصطدم ببريده الحالي.
        var user = TestDataSeeder.CreateUser(303, "عميل_ج", UserRole.Client, email: "same@test.local");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            FullName = "عميل_ج معدّل",
            Role = UserRole.Client,
            Phone = user.Phone,
            Email = "same@test.local"
        };

        await _userService.UpdateAsync(303, dto);

        var updated = await _context.Users.FindAsync(303);
        updated!.FullName.Should().Be("عميل_ج معدّل");
        updated.Email.Should().Be("same@test.local");
    }

    [Fact]
    public async Task UpdateAsync_WithoutEmail_KeepsTheExistingOne()
    {
        var user = TestDataSeeder.CreateUser(304, "عميل_د", UserRole.Client, email: "keep@test.local");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            FullName = "عميل_د معدّل",
            Role = UserRole.Client,
            Phone = user.Phone,
            Email = null
        };

        await _userService.UpdateAsync(304, dto);

        var updated = await _context.Users.FindAsync(304);
        updated!.FullName.Should().Be("عميل_د معدّل");
        updated.Email.Should().Be("keep@test.local", "غياب البريد يعني «أبقِ الحالي» لا «امسحه»");
    }

    // ─────────────────────────────────────────────
    // GetPagedAsync — فلتر isActive على الخادم
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_IsActiveFilter_IsAppliedBeforePaging()
    {
        // كان الفلتر في الويب على الذاكرة بعد جلب صفحة واحدة (PageSize=100)،
        // فيسقط الحساب النشط إن وقع خارج تلك الصفحة. هنا الفلتر على الخادم.
        _context.Users.AddRange(
            TestDataSeeder.CreateDriver(310, "سائق_نشط", 1, isActive: true),
            TestDataSeeder.CreateDriver(311, "سائق_موقوف", 1, isActive: false),
            TestDataSeeder.CreateDriver(312, "سائق_موقوف_٢", 1, isActive: false));
        await _context.SaveChangesAsync();

        var pagination = new PaginationParams { PageSize = 100 };

        var active = await _userService.GetPagedAsync(
            UserRole.Driver, factoryId: null, driverStatus: null, pagination, isActive: true);
        var inactive = await _userService.GetPagedAsync(
            UserRole.Driver, factoryId: null, driverStatus: null, pagination, isActive: false);

        active.Items.Should().ContainSingle()
            .Which.FullName.Should().Be("سائق_نشط");
        inactive.Items.Should().HaveCount(2);
        inactive.TotalCount.Should().Be(2, "العدّ الكلي يُحسب بعد التصفية على الخادم");

        // بلا الفلتر: الجميع
        var all = await _userService.GetPagedAsync(
            UserRole.Driver, factoryId: null, driverStatus: null, pagination);
        all.TotalCount.Should().Be(3, "isActive = null يعني بلا تصفية");
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
