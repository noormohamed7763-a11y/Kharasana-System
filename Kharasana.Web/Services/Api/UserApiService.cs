using Kharasana.Application.Common;
using Kharasana.Web.Localization;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Web.Services.Interfaces;
using System.Net;

namespace Kharasana.Web.Services.Api;

public class UserApiService : IUserApiService
{
    private readonly ApiClient _apiClient;

    public UserApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task CreateAsync(CreateUserViewModel model)
    {
        var response = await _apiClient.PostAsync<ApiResponse<object>>("Users", model);

        if (response == null || !response.Success)
        {
            throw new ApiServiceException(
                HttpStatusCode.BadRequest,
                ApiErrorCatalog.UserCreateFailed,
                new object[] { response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task<PagedResult<UserListItemViewModel>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        int? factoryId = null)
    {
        var query = $"Users?pageNumber={pageNumber}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search)) query += $"&Search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(role)) query += $"&Role={Uri.EscapeDataString(role)}";
        if (factoryId.HasValue) query += $"&FactoryId={factoryId.Value}";

        var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(query);

        if (response == null || !response.Success || response.Data == null)
        {
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError);
        }

        return new PagedResult<UserListItemViewModel>
        {
            PageNumber = response.Data.PageNumber,
            PageSize = response.Data.PageSize,
            TotalCount = response.Data.TotalCount,
            Items = response.Data.Items.Select(u => new UserListItemViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                WhatsApp = u.WhatsApp,
                Role = u.Role,
                FactoryId = u.FactoryId,
                IsActive = u.IsActive,
                LicenseNumber = u.LicenseNumber,
                DriverStatus = u.DriverStatus?.ToString()
            }).ToList()
        };
    }

    public async Task<UserListItemViewModel> GetUserByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{id}");

        if (response == null || !response.Success || response.Data == null)
        {
            throw new ApiServiceException(HttpStatusCode.NotFound, ApiErrorCatalog.UserNotFound, new object[] { id });
        }

        var u = response.Data;
        return new UserListItemViewModel
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            WhatsApp = u.WhatsApp,
            Role = u.Role,
            FactoryId = u.FactoryId,
            FactoryName = u.FactoryName,
            IsActive = u.IsActive,
            LicenseNumber = u.LicenseNumber,
            DriverStatus = u.DriverStatus?.ToString(),
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }

    public async Task UpdateAsync(int id, UpdateUserViewModel model)
    {
        var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}", model);

        if (response == null || !response.Success)
        {
            throw new ApiServiceException(
                HttpStatusCode.BadRequest,
                ApiErrorCatalog.UserUpdateFailed,
                new object[] { id, response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task DeleteAsync(int id)
    {
        var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Users/{id}");

        if (response == null || !response.Success)
        {
            throw new ApiServiceException(
                HttpStatusCode.BadRequest,
                ApiErrorCatalog.UserUpdateFailed, // أو خطأ حذف خاص
                new object[] { id, response?.Message ?? "سبب غير معروف" });
        }
    }

    public async Task<(int Admins, int FactoryEmployees, int Drivers)> GetRoleCountsAsync(
        string? search,
        int? factoryId)
    {
        string CountQuery(string role) =>
            $"Users?pageNumber=1&pageSize=1&Role={role}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
            + (factoryId.HasValue ? $"&FactoryId={factoryId.Value}" : "");

        var admins = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.Admin));
        var employees = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.FactoryEmployee));
        var drivers = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.Driver));

        return (admins, employees, drivers);
    }
}
