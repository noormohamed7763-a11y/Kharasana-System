using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Application.DTOs.User;

namespace Kharasana.Web.Services.Api;

public class UserApiService : IUserApiService
{
    private readonly ApiClient _apiClient;

    public UserApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<object>> CreateAsync(CreateUserViewModel model)
    {
        try
        {
            var response = await _apiClient.PostAsync<ApiResponse<object>>("Users", model);

            if (response == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "تعذر الاتصال بالخادم الرئيسي. يرجى المحاولة مرة أخرى."
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "خطأ في الاتصال بالخادم. تأكد من تشغيل الـ API."
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى."
            };
        }
    }

    public async Task<(int Admins, int FactoryEmployees, int Drivers)> GetRoleCountsAsync(
        string? search,
        int? factoryId)
    {
        // تنفيذ متسلسل — لا نجعل ApiClient يشارك رأس Authorization بين طلبات متزامنة
        string CountQuery(string role) =>
            $"Users?pageNumber=1&pageSize=1&Role={role}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
            + (factoryId.HasValue ? $"&FactoryId={factoryId.Value}" : "");

        var admins = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery("Admin"));
        var employees = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery("FactoryEmployee"));
        var drivers = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery("Driver"));

        return (admins, employees, drivers);
    }

    public async Task<PagedResult<UserListItemViewModel>?> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        int? factoryId = null)
    {
        var query = $"Users?pageNumber={pageNumber}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            query += $"&Search={Uri.EscapeDataString(search)}";

        if (!string.IsNullOrWhiteSpace(role))
            query += $"&Role={Uri.EscapeDataString(role)}";

        if (factoryId.HasValue)
            query += $"&FactoryId={factoryId.Value}";

        var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(query);

        if (response == null || !response.Success || response.Data == null)
            return null;

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
            })
        };
    }

    public async Task<UserListItemViewModel?> GetUserByIdAsync(int id)
    {
        try
        {
            var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{id}");

            if (response == null || !response.Success || response.Data == null)
                return null;

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
                IsActive = u.IsActive,
                LicenseNumber = u.LicenseNumber,
                DriverStatus = u.DriverStatus?.ToString()
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResponse<object>> UpdateAsync(int id, UpdateUserViewModel model)
    {
        try
        {
            var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}", model);

            if (response == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "تعذر الاتصال بالخادم الرئيسي. يرجى المحاولة مرة أخرى."
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "خطأ في الاتصال بالخادم. تأكد من تشغيل الـ API."
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى."
            };
        }
    }

    public async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        try
        {
            var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Users/{id}");

            if (response == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "تعذر الاتصال بالخادم الرئيسي. يرجى المحاولة مرة أخرى."
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "خطأ في الاتصال بالخادم. تأكد من تشغيل الـ API."
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى."
            };
        }
    }
}
