using Kharasana.Application.Common;
using Kharasana.Web.Models.Users;
using Kharasana.Web.ViewModels.Users;

namespace Kharasana.Web.Services.Interfaces;

public interface IUserApiService
{
    Task<ApiResponse<object>> CreateAsync(CreateUserViewModel model);

    Task<PagedResult<UserListItemViewModel>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        int? factoryId = null);

    Task<UserListItemViewModel?> GetUserByIdAsync(int id);

    Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserViewModel model);

    Task<ApiResponse<object>> DeleteAsync(int id);
}