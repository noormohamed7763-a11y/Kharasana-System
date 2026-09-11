using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, PaginationParams pagination)
    {
        var (items, totalCount) = await _unitOfWork.Users.GetPagedAsync(
            role, factoryId, driverStatus, pagination.Search, pagination.PageNumber, pagination.PageSize);

        return new PagedResult<UserDto>
        {
            Items = items.Select(MapToDto),
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException(Messages.UserNotFound);

        return MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (dto.Role == UserRole.Admin)
            throw new ForbiddenException(Messages.CannotCreateAdmin);

        if (string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(dto.Phone))
            throw new BusinessException(Messages.EmailOrPhoneRequired);

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var emailExists = await _unitOfWork.Users.EmailExistsAsync(dto.Email);
            if (emailExists)
                throw new ConflictException(Messages.EmailAlreadyExists);
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            normalizedPhone = YemeniPhoneHelper.Normalize(dto.Phone);
            if (normalizedPhone == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);

            var phoneExists = await _unitOfWork.Users.PhoneExistsAsync(normalizedPhone);
            if (phoneExists)
                throw new ConflictException(Messages.PhoneAlreadyExists);
        }

        string? normalizedWhatsApp = null;
        if (!string.IsNullOrWhiteSpace(dto.WhatsApp))
        {
            normalizedWhatsApp = YemeniPhoneHelper.Normalize(dto.WhatsApp);
            if (normalizedWhatsApp == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);
        }

        await ValidateUserRoleAsync(
            dto.Role,
            dto.FactoryId,
            dto.LicenseNumber,
            null);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Phone = normalizedPhone,
            WhatsApp = normalizedWhatsApp,
            Role = dto.Role,
            LicenseNumber = dto.LicenseNumber,
            DriverStatus = dto.Role == UserRole.Driver
                ? (dto.DriverStatus ?? DriverStatus.Offline)
                : null,
            FactoryId = dto.Role == UserRole.Client
                ? null
                : dto.FactoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException(Messages.UserNotFound);

        string? normalizedPhone = user.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            normalizedPhone = YemeniPhoneHelper.Normalize(dto.Phone);
            if (normalizedPhone == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);

            if (normalizedPhone != user.Phone)
            {
                var phoneExists = await _unitOfWork.Users.PhoneExistsAsync(normalizedPhone);
                if (phoneExists)
                    throw new ConflictException(Messages.PhoneAlreadyExists);
            }
        }

        string? normalizedWhatsApp = user.WhatsApp;
        if (!string.IsNullOrWhiteSpace(dto.WhatsApp))
        {
            normalizedWhatsApp = YemeniPhoneHelper.Normalize(dto.WhatsApp);
            if (normalizedWhatsApp == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);
        }

        user.FullName = dto.FullName;
        user.Phone = normalizedPhone;
        user.WhatsApp = normalizedWhatsApp;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (dto.Role == UserRole.Admin)
            throw new ForbiddenException(Messages.CannotChangeToAdmin);

        // ✅ لا يسمح بتغيير دور السائق إلى دور آخر (حماية إضافية)
        if (user.Role == UserRole.Driver && dto.Role != UserRole.Driver)
        {
            throw new BusinessException("لا يمكن تغيير دور السائق.");
        }

        await ValidateUserRoleAsync(
            dto.Role,
            dto.FactoryId,
            dto.LicenseNumber,
            id);

        string? normalizedPhone = user.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            normalizedPhone = YemeniPhoneHelper.Normalize(dto.Phone);
            if (normalizedPhone == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);

            if (normalizedPhone != user.Phone)
            {
                var phoneExists = await _unitOfWork.Users.PhoneExistsAsync(normalizedPhone);
                if (phoneExists)
                    throw new ConflictException(Messages.PhoneAlreadyExists);
            }
        }

        string? normalizedWhatsApp = user.WhatsApp;
        if (!string.IsNullOrWhiteSpace(dto.WhatsApp))
        {
            normalizedWhatsApp = YemeniPhoneHelper.Normalize(dto.WhatsApp);
            if (normalizedWhatsApp == null)
                throw new BusinessException(Messages.InvalidYemeniPhone);
        }

        user.FullName = dto.FullName;
        user.Phone = normalizedPhone;
        user.WhatsApp = normalizedWhatsApp;
        user.ProfileImage = dto.ProfileImage;
        user.Role = dto.Role;
        user.LicenseNumber = dto.LicenseNumber;
        user.DriverStatus = dto.Role == UserRole.Driver
            ? (dto.DriverStatus ?? user.DriverStatus ?? DriverStatus.Offline)
            : null;
        user.FactoryId = dto.Role == UserRole.Client
            ? null
            : dto.FactoryId;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (user.Role == UserRole.Admin)
            throw new ForbiddenException(Messages.CannotDeleteAdmin);

        // ✅ تعطيل السائق بدلاً من حذفه نهائياً
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateDriverStatusAsync(
        int driverId, UpdateDriverStatusDto dto, UserRole callerRole, int? callerFactoryId)
    {
        var driver = await _unitOfWork.Users.GetByIdAsync(driverId);
        if (driver == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (driver.Role != UserRole.Driver)
            throw new BusinessException(Messages.InvalidDriver);

        // ✅ التحقق من صلاحية الموظف: فقط سائقي مصنعه
        if (callerRole == UserRole.FactoryEmployee)
        {
            if (!callerFactoryId.HasValue || driver.FactoryId != callerFactoryId.Value)
                throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);
        }

        driver.DriverStatus = dto.DriverStatus;
        driver.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(driver);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleDriverActiveAsync(
        int driverId, UserRole callerRole, int? callerFactoryId)
    {
        var driver = await _unitOfWork.Users.GetByIdAsync(driverId);
        if (driver == null)
            throw new NotFoundException(Messages.UserNotFound);

        if (driver.Role != UserRole.Driver)
            throw new BusinessException(Messages.InvalidDriver);

        // ✅ التحقق من صلاحية الموظف: فقط سائقي مصنعه
        if (callerRole == UserRole.FactoryEmployee)
        {
            if (!callerFactoryId.HasValue || driver.FactoryId != callerFactoryId.Value)
                throw new ForbiddenException(Messages.FactoryEmployeeFactoryMismatch);
        }

        driver.IsActive = !driver.IsActive;
        driver.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(driver);
        await _unitOfWork.SaveChangesAsync();

        return driver.IsActive;
    }

    private async Task ValidateUserRoleAsync(
        UserRole role,
        int? factoryId,
        string? licenseNumber,
        int? excludeUserId = null)
    {
        if (role == UserRole.Client)
            return;

        if (role == UserRole.Driver && !factoryId.HasValue)
            throw new BusinessException(Messages.FactoryRequiredForDriver);

        if (role == UserRole.FactoryEmployee && !factoryId.HasValue)
            throw new BusinessException(Messages.FactoryRequiredForEmployee);

        if (factoryId.HasValue)
        {
            var factory = await _unitOfWork.Factories.GetByIdAsync(factoryId.Value);
            if (factory == null || factory.IsDeleted)
                throw new NotFoundException(Messages.FactoryNotFound);

            // ✅ منع وجود أكثر من حساب FactoryEmployee لنفس المصنع
            if (role == UserRole.FactoryEmployee)
            {
                var hasAccount = await _unitOfWork.Users.FactoryHasAccountAsync(
                    factoryId.Value,
                    excludeUserId);

                if (hasAccount)
                {
                    throw new ConflictException(Messages.FactoryAlreadyHasAccount);
                }
            }
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            WhatsApp = user.WhatsApp,
            ProfileImage = user.ProfileImage,
            Role = user.Role.ToString(),
            LicenseNumber = user.LicenseNumber,
            DriverStatus = user.DriverStatus,
            FactoryId = user.FactoryId,
            IsActive = user.IsActive
        };
    }
}