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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kharasana.Tests.Tests;

/// <summary>
/// اختبارات التحقق من صلاحيات الدخول — التركيز على:
/// • المصنع المحذوف (مؤرشف) يمنع دخول موظف/سائق
/// • المصنع الموقوف يسمح بالدخول مع تحذير
/// • السائق الموقوف يُمنع بالرسالة الخاصة
/// • حماية الحساب من التخمين العنيف (قفل بعد 5 محاولات)
/// • إعادة تعيين العدّاد بعد نجاح تسجيل الدخول
/// • RegisterAsync — التسجيل الذاتي: تطابق كلمتي المرور، تفرّد البريد والهاتف، دور العميل دائماً
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

    private static LoginRequestDto MakeLogin(string emailOrPhone, string? password = null)
        => new() { EmailOrPhone = emailOrPhone, Password = password ?? TestDataSeeder.TestPassword };

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
        var act = () => _authService.LoginAsync(MakeLogin(employee.Email!));

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
        var act = () => _authService.LoginAsync(MakeLogin(driver.Phone!));

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
        var result = await _authService.LoginAsync(MakeLogin(employee.Email!));

        // Assert — نجاح الدخول + تحذير موجود + IsActive = false
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Notification.Should().Be(Messages.FactoryInactiveLogin);
        result.Data.FactoryIsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ActiveFactory_Employee_SucceedsWithoutNotification()
    {
        // Arrange
        var factory = TestDataSeeder.CreateFactory(3, "نشيطة", isActive: true);
        var employee = TestDataSeeder.CreateUser(30, "موظف نشط", UserRole.FactoryEmployee, factoryId: 3);
        _context.Factories.Add(factory);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(MakeLogin(employee.Email!));

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
        var act = () => _authService.LoginAsync(MakeLogin(driver.Phone!));

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
        var act = () => _authService.LoginAsync(MakeLogin(client.Phone!));

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
        await Assert.ThrowsAsync<BusinessException>(() => _authService.LoginAsync(MakeLogin(user.Phone!, " WRONG ")));

        // Assert
        var updated = await _context.Users.FindAsync(60);
        updated!.FailedLoginAttempts.Should().Be(1);
        updated.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Login_FailedAttempts_ReachesLockout()
    {
        // Arrange — مستخدم مع 4 محاولات فاشلة سابقة
        var user = TestDataSeeder.CreateUser(61, "على وشك القفل", UserRole.Client, phone: "770000061");
        user.FailedLoginAttempts = 4;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act — المحاولة الخامسة الفاشلة تُفعّل القفل
        await Assert.ThrowsAsync<BusinessException>(() => _authService.LoginAsync(MakeLogin(user.Phone!, " WRONG ")));

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

        // Act — كلمة المرور صحيحة لكن الحساب مقفل
        var act = () => _authService.LoginAsync(MakeLogin(user.Phone!));

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
        var user = TestDataSeeder.CreateUser(70, "يعود للعمل", UserRole.Client, phone: "770000070");
        user.FailedLoginAttempts = 3;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _authService.LoginAsync(MakeLogin(user.Phone!));

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
        var act = () => _authService.LoginAsync(MakeLogin("050000000", "any"));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    /// <summary>
    /// null صريح في EmailOrPhone (كما يرسل عميل متهالك {"emailOrPhone": null}) يجب أن يُردّ
    /// بـ 400 لا 500 — كان Trim() ينفجر بـ NullReferenceException قبل هذا الحارس.
    /// </summary>
    [Fact]
    public async Task Login_NullIdentifier_ThrowsInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequestDto { EmailOrPhone = null!, Password = TestDataSeeder.TestPassword };

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    /// <summary>
    /// null صريح في Password لمستخدم موجود يجب أن يُردّ بـ 400 لا 500 —
    /// كان BCrypt.Verify(null, ...) يرمي ArgumentNullException بعد العثور على المستخدم.
    /// </summary>
    [Fact]
    public async Task Login_ExistingUser_NullPassword_ThrowsInvalidCredentials()
    {
        // Arrange — مستخدم موجود حتى يصل التنفيذ إلى التحقق من كلمة المرور
        var user = TestDataSeeder.CreateUser(80, "بلا كلمة مرور", UserRole.Client, phone: "770000080");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequestDto { EmailOrPhone = user.Phone!, Password = null! };

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    // ─────────────────────────────────────────────
    // كشف حالة الحساب — محصور بمن يعرف كلمة المرور
    //
    // كانت فحوص النشاط وأرشفة المصنع والقفل تُنفَّذ قبل التحقق من كلمة المرور،
    // فيكشف نصّ الردّ — لمن لا يعرف كلمة المرور — أن الحساب موجود وأنه سائق
    // موقوف أو أن مصنعه مؤرشف. الاختبارات أعلاه (Login_DeactivatedDriver …
    // Login_ArchivedFactory_…) تستدعي MakeLogin بلا وسيط كلمة مرور، أي بكلمة
    // المرور الصحيحة — فهي تُثبت أن الرسائل المخصّصة باقية لمن يستحقّها،
    // وهذه تُثبت أن غير المالك لا يحصل عليها.
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Login_DeactivatedDriver_WrongPassword_DoesNotRevealAccountState()
    {
        // Arrange
        var driver = TestDataSeeder.CreateDriver(90, "سائق موقوف", factoryId: 1, isActive: false);
        _context.Users.Add(driver);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(MakeLogin(driver.Phone!, " WRONG "));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    [Fact]
    public async Task Login_InactiveClient_WrongPassword_DoesNotRevealAccountState()
    {
        // Arrange
        var client = TestDataSeeder.CreateUser(91, "عميل موقوف", UserRole.Client, isActive: false, phone: "770000091");
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(MakeLogin(client.Phone!, " WRONG "));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    [Fact]
    public async Task Login_ArchivedFactory_WrongPassword_DoesNotRevealAccountState()
    {
        // Arrange — مصنع مؤرشف + موظف مرتبط به (بنية اختبار FactoryArchived أعلاه)
        var factory = TestDataSeeder.CreateFactory(4, "مؤرشفة", isActive: false, isDeleted: true);
        var employee = TestDataSeeder.CreateUser(92, "موظف مصنع مؤرشف", UserRole.FactoryEmployee, factoryId: 4);
        _context.Factories.Add(factory);
        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(MakeLogin(employee.Email!, " WRONG "));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    /// <summary>
    /// حساب مقفل + كلمة مرور خاطئة ← رسالة الاعتماد العامة لا رسالة القفل: القفل
    /// لا يُكشف إلا لمن أثبت كلمة مروره. ومع ذلك لا يُمَدّ القفل — المحاولة الفاشلة
    /// على حساب مقفل لا تُسجَّل، فلا يُطيل أحد قفل غيره، ولا يُطيل صاحب الحساب قفله
    /// بخطأ مطبعي أثناء انتهاء المدة.
    /// </summary>
    [Fact]
    public async Task Login_LockedAccount_WrongPassword_StaysGenericAndKeepsLockUnchanged()
    {
        // Arrange
        var user = TestDataSeeder.CreateUser(93, "حساب مقفل", UserRole.Client, phone: "770000093");
        user.FailedLoginAttempts = 5;
        var lockoutEnd = DateTime.UtcNow.AddMinutes(10);
        user.LockoutEnd = lockoutEnd;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.LoginAsync(MakeLogin(user.Phone!, " WRONG "));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);

        var updated = await _context.Users.FindAsync(93);
        updated!.FailedLoginAttempts.Should().Be(5, "المحاولة على حساب مقفل أصلاً لا تُسجَّل");
        updated.LockoutEnd.Should().Be(lockoutEnd, "القفل لا يُمَدّ بمحاولة فاشلة إضافية");
    }

    /// <summary>
    /// بريد غير مسجَّل يمرّ بمسار التجزئة الصورية (VerifyDummy) — ويجب أن يرمي
    /// BusinessException لا استثناء BCrypt. لو كُتبت التجزئة الصورية بصيغة غير
    /// صالحة لـ BCrypt لرمى Verify استثناءً، فيتحوّل كل دخول بمعرّف غير مسجَّل إلى 500.
    /// </summary>
    [Fact]
    public async Task Login_UnknownEmail_ThrowsInvalidCredentialsNotBcryptError()
    {
        // Act
        var act = () => _authService.LoginAsync(MakeLogin("no-such-user@example.com", "any"));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.InvalidCredentials);
    }

    // ─────────────────────────────────────────────
    // ⑥  RegisterAsync — التسجيل الذاتي (نقطة عامة بلا توكن)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Register_PasswordMismatch_ThrowsPasswordsNotMatch()
    {
        // Arrange — تأكيد كلمة المرور لا يطابق كلمة المرور
        var dto = new RegisterUserDto
        {
            FullName = "مستخدم جديد",
            Phone = "771120001",
            Password = TestDataSeeder.TestPassword,
            ConfirmPassword = "Different@9999"
        };

        // Act
        var act = () => _authService.RegisterAsync(dto);

        // Assert — الرفض قبل أي فحص لقاعدة البيانات وقبل إنشاء أي حساب
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.PasswordsNotMatch);

        (await _context.Users.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// الخدمة تفرض الحد الأدنى لطول كلمة المرور بنفسها — لم يكن هناك أي فحص في
    /// AuthService (كان الحارس الوحيد سمة [MinLength(6)] على الـ DTO، أضعف من
    /// بقية المسارات وتسقط إن استُدعيت الخدمة من مسار لا يمرّ على ModelState).
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("1234567")]  // محرف أقل من الحد الأدنى الموحّد
    [InlineData("")]
    public async Task Register_PasswordBelowMinimum_ThrowsBusinessException(string password)
    {
        var dto = new RegisterUserDto
        {
            FullName = "عميل بكلمة مرور قصيرة",
            Phone = "771120009",
            Password = password,
            ConfirmPassword = password
        };

        var act = () => _authService.RegisterAsync(dto);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage(Messages.PasswordMinLength);

        (await _context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Register_ValidData_CreatesActiveClientWithoutFactory()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            FullName = "عميل جديد",
            Email = "new-client@test.local",
            Phone = "771120005",
            WhatsApp = "771120005",
            Password = TestDataSeeder.TestPassword,
            ConfirmPassword = TestDataSeeder.TestPassword
        };

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert — التسجيل الذاتي يُنتج دائماً عميلاً نشطاً بلا مصنع (لا تصعيد صلاحيات)
        result.Success.Should().BeTrue();

        var created = await _context.Users.SingleAsync(u => u.Email == "new-client@test.local");
        created.Role.Should().Be(UserRole.Client);
        created.FactoryId.Should().BeNull();
        created.IsActive.Should().BeTrue();
        // يُخزَّن الرقم بالصيغة القياسية 967XXXXXXXXX — وهي الصيغة التي يبحث بها تسجيل الدخول
        created.Phone.Should().Be("967771120005");
    }

    [Fact]
    public async Task Register_DuplicatePhone_ThrowsConflict()
    {
        // Arrange — الرقم مسجّل مسبقاً لحساب آخر
        var existing = TestDataSeeder.CreateUser(
            120, "عميل مسجّل", UserRole.Client, phone: "771120002");
        _context.Users.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new RegisterUserDto
        {
            FullName = "عميل مكرر",
            Phone = "771120002",
            Password = TestDataSeeder.TestPassword,
            ConfirmPassword = TestDataSeeder.TestPassword
        };

        // Act
        var act = () => _authService.RegisterAsync(dto);

        // Assert — 409 (لا حساب ثانٍ بنفس الرقم) — العقد المُعلن في AuthController.Register
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.PhoneAlreadyExists);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        // Arrange — البريد مسجّل مسبقاً لحساب آخر
        var existing = TestDataSeeder.CreateUser(
            121, "عميل مسجّل", UserRole.Client,
            phone: "771120003", email: "dup-register@test.local");
        _context.Users.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new RegisterUserDto
        {
            FullName = "عميل مكرر",
            Email = "dup-register@test.local",
            Phone = "771120004",
            Password = TestDataSeeder.TestPassword,
            ConfirmPassword = TestDataSeeder.TestPassword
        };

        // Act
        var act = () => _authService.RegisterAsync(dto);

        // Assert — 409 (لا حساب ثانٍ بنفس البريد) — العقد المُعلن في AuthController.Register
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(Messages.EmailAlreadyExists);
    }

    public void Dispose() => _context.Dispose();
}