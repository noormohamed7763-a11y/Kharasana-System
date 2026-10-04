using Kharasana.Application.Common;
using Kharasana.Web.Localization;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Web.Services.Interfaces;

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
                    Message = AppMessages.Common.MainServerUnreachable
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.ApiNotRunning
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.UnexpectedError
            };
        }
    }

    public async Task<(int Admins, int FactoryEmployees, int Drivers)> GetRoleCountsAsync(
        string? search,
        int? factoryId)
    {
        // تنفيذ متسلسل. لا خطر من التوازي: ApiClient يضيف رأس Authorization على
        // HttpRequestMessage نفسه لا على DefaultRequestHeaders — وطلب التوازي هنا
        // تغيير سلوكي لم يُطلب، فيبقى التسلسل كما هو.
        string CountQuery(string role) =>
            $"Users?pageNumber=1&pageSize=1&Role={role}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
            + (factoryId.HasValue ? $"&FactoryId={factoryId.Value}" : "");

        var admins = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.Admin));
        var employees = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.FactoryEmployee));
        var drivers = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(Roles.Driver));

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
                // اسم المصنع والتواريخ: تعرضها صفحة تفاصيل المستخدم، وكانت تُقرأ منها
                // بلا أن تُنقل من الـ API ⇒ كانت «المصنع» تعرض المعرّف الرقمي و«تاريخ
                // الإنشاء» شرطة دائماً و«آخر تحديث» لا يظهر أبداً.
                FactoryName = u.FactoryName,
                IsActive = u.IsActive,
                LicenseNumber = u.LicenseNumber,
                DriverStatus = u.DriverStatus?.ToString(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
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
                    Message = AppMessages.Common.MainServerUnreachable
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.ApiNotRunning
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.UnexpectedError
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
                    Message = AppMessages.Common.MainServerUnreachable
                };
            }

            return response;
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.ApiNotRunning
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Message = AppMessages.Common.UnexpectedError
            };
        }
    }
}
