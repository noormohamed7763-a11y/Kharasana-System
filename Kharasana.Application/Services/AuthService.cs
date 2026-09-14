using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class AuthService : IAuthService
{
    // ✅ إعدادات القفل (الحماية من التخمين العنيف) — بدون اعتماد على IConfiguration
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<object>> RegisterAsync(RegisterUserDto request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new BusinessException(Messages.PasswordsNotMatch);

        // ✅ التحقق من وجود البريد الإلكتروني قبل الاستخدام
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailExists = await _unitOfWork.Users.EmailExistsAsync(request.Email);
            if (emailExists)
                throw new ConflictException(Messages.EmailAlreadyExists);
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            normalizedPhone = YemeniPhoneHelper.Normalize(request.Phone);
            if (normalizedPhone == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);

            var phoneExists = await _unitOfWork.Users.PhoneExistsAsync(normalizedPhone);
            if (phoneExists)
                throw new ConflictException(Messages.PhoneAlreadyExists);
        }

        string? normalizedWhatsApp = null;
        if (!string.IsNullOrWhiteSpace(request.WhatsApp))
        {
            normalizedWhatsApp = YemeniPhoneHelper.Normalize(request.WhatsApp);
            if (normalizedWhatsApp == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,  // ✅ يمكن أن يكون null
            PasswordHash = _passwordHasher.Hash(request.Password),
            Phone = normalizedPhone,
            WhatsApp = normalizedWhatsApp,
            Role = UserRole.Client,
            FactoryId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new ApiResponse<object>
        {
            Success = true,
            Message = Messages.RegisterSuccess,
            Data = new { UserId = user.UserId }
        };
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var identifier = request.EmailOrPhone.Trim();

        User? user;

        if (identifier.Contains('@'))
        {
            user = await _unitOfWork.Users.GetByEmailAsync(identifier);
        }
        else
        {
            var normalizedPhone = YemeniPhoneHelper.Normalize(identifier);
            if (normalizedPhone == null)
                throw new BusinessException(Messages.InvalidCredentials);

            user = await _unitOfWork.Users.GetByPhoneAsync(normalizedPhone);
        }

        if (user == null)
            throw new BusinessException(Messages.InvalidCredentials);

        if (!user.IsActive)
        {
            // ✅ رسالة مخصصة للسائقين الموقوفين (حذف السائق = تعطيل مؤقت حتى يعيد موظف المصنع تفعيله)
            if (user.Role == UserRole.Driver)
                throw new BusinessException(Messages.DriverDeactivated);

            throw new BusinessException(Messages.UserInactive);
        }

        // ✅ التحقق من حالة المصنع لموظفي المصنع والسائقين
        string? notification = null;
        bool? factoryIsActive = null;
        if (user.FactoryId.HasValue &&
            (user.Role == UserRole.FactoryEmployee || user.Role == UserRole.Driver))
        {
            // ✅ قراءة شاملة (بما فيها المؤرشف) حتى نستطيع التمييز: مصنع مؤرشف ← منع الدخول برسالة واضحة
            var factory = await _unitOfWork.Factories.GetByIdIncludingDeletedAsync(user.FactoryId.Value);
            if (factory != null)
            {
                if (factory.IsDeleted)
                {
                    // المصنع محذوف (مؤرشف) — منع تسجيل الدخول تماماً
                    throw new BusinessException(Messages.FactoryArchived);
                }

                factoryIsActive = factory.IsActive;
                if (!factory.IsActive)
                {
                    // المصنع موقوف (غير نشط) — يُسمح بالدخول مع عرض تحذير
                    notification = Messages.FactoryInactiveLogin;
                }
            }
        }

        // ✅ الحماية من التخمين العنيف: التحقق من قفل الحساب قبل التحقق من كلمة المرور
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            throw new BusinessException(Messages.AccountLocked);

        var validPassword = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            await RecordFailedAttemptAsync(user);
            throw new BusinessException(Messages.InvalidCredentials);
        }

        // ✅ نجاح تسجيل الدخول: تصفير عدّاد المحاولات وإلغاء القفل
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var tokenResult = _tokenService.GenerateToken(user);
        var response = new LoginResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            FactoryId = user.FactoryId,
            Token = tokenResult.Token,
            Expiration = tokenResult.Expiration,
            Notification = notification,
            FactoryIsActive = factoryIsActive
        };

        return new ApiResponse<LoginResponseDto>
        {
            Success = true,
            Message = Messages.LoginSuccess,
            Data = response
        };
    }

    private async Task RecordFailedAttemptAsync(User user)
    {
        user.FailedLoginAttempts++;

        // ✅ قفل الحساب عند بلوغ الحد الأقصى للمحاولات الفاشلة
        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}