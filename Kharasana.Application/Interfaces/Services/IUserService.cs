using Kharasana.Application.Common;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();

    Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto);

    Task<PagedResult<UserDto>> GetPagedAsync(
        UserRole? role, int? factoryId, DriverStatus? driverStatus, PaginationParams pagination);

    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<bool> UpdateAsync(int id, UpdateUserDto dto);
    Task<bool> DeleteAsync(int id);

    Task<bool> UpdateDriverStatusAsync(
        int driverId, UpdateDriverStatusDto dto, UserRole callerRole, int? callerFactoryId);

    Task<bool> ToggleDriverActiveAsync(
        int driverId, UserRole callerRole, int? callerFactoryId);
}