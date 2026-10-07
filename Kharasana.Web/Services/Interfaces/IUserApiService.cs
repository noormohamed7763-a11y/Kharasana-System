using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Users;

namespace Kharasana.Web.Services.Interfaces;

public interface IUserApiService
{
    Task CreateAsync(CreateUserViewModel model);

    Task<PagedResult<UserListItemViewModel>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        int? factoryId = null);

    Task<(int Admins, int FactoryEmployees, int Drivers)> GetRoleCountsAsync(
        string? search,
        int? factoryId);

    Task<UserListItemViewModel> GetUserByIdAsync(int id);

    Task UpdateAsync(int id, UpdateUserViewModel model);

    Task DeleteAsync(int id);
}