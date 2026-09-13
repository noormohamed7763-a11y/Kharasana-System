using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Drivers;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api;

/// <summary>
/// خدمة السائقين - تنفيذ واجهة IDriverApiService
/// </summary>
public class DriverApiService : IDriverApiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<DriverApiService> _logger;

    // ✅ مهلة الطلب (بالثواني)
    private const int RequestTimeoutSeconds = 30;

    public DriverApiService(ApiClient apiClient, ILogger<DriverApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<DriverListItemViewModel>?> GetDriversAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ بناء رابط الطلب
            var query = $"Users?role={(int)UserRole.Driver}&PageNumber={pageNumber}&PageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(search))
                query += $"&Search={Uri.EscapeDataString(search)}";

            if (factoryId.HasValue)
                query += $"&factoryId={factoryId.Value}";

            _logger.LogDebug("Fetching drivers with query: {Query}", query);

            // ✅ إرسال الطلب مع مهلة
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(query, cts.Token);

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("GetDriversAsync: API returned null/failed for query {Query}", query);
                return null;
            }

            // ✅ تحويل البيانات
            return new PagedResult<DriverListItemViewModel>
            {
                PageNumber = response.Data.PageNumber,
                PageSize = response.Data.PageSize,
                TotalCount = response.Data.TotalCount,
                Items = response.Data.Items.Select(u => new DriverListItemViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    LicenseNumber = u.LicenseNumber,
                    FactoryId = u.FactoryId,
                    DriverStatus = u.DriverStatus,
                    IsActive = u.IsActive
                })
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetDriversAsync: Request was cancelled or timed out.");
            return null;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetDriversAsync");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<DriverListItemViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{id}", cts.Token);

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("GetByIdAsync: Could not load driver {DriverId}", id);
                return null;
            }

            var u = response.Data;

            return new DriverListItemViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Phone = u.Phone,
                LicenseNumber = u.LicenseNumber,
                FactoryId = u.FactoryId,
                DriverStatus = u.DriverStatus,
                IsActive = u.IsActive
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetByIdAsync: Request was cancelled or timed out for driver {DriverId}", id);
            return null;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetByIdAsync for driverId={DriverId}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<EditDriverViewModel?> GetForEditAsync(int driverId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{driverId}", cts.Token);

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("GetForEditAsync: Could not load driver {DriverId}", driverId);
                return null;
            }

            var u = response.Data;

            return new EditDriverViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                LicenseNumber = u.LicenseNumber,
                ProfileImage = u.ProfileImage,
                FactoryId = u.FactoryId,
                IsActive = u.IsActive
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetForEditAsync: Request was cancelled or timed out for driver {DriverId}", driverId);
            return null;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetForEditAsync for driverId={DriverId}", driverId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Message)> CreateAsync(CreateDriverViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ استخدام PascalCase لتطابق CreateUserDto في الـ API
            var payload = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Phone = model.Phone,
                Role = (int)UserRole.Driver,
                LicenseNumber = model.LicenseNumber,
                DriverStatus = (int)DriverStatus.Offline,
                FactoryId = model.FactoryId
            };

            _logger.LogDebug("Creating new driver: {FullName}", model.FullName);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.PostAsync<ApiResponse<object>>("Users", payload, cts.Token);

            if (response == null)
            {
                _logger.LogWarning("CreateAsync (Driver): API returned null response.");
                return (false, "تعذر الاتصال بالخادم. حاول مرة أخرى.");
            }

            if (!response.Success)
            {
                _logger.LogWarning("CreateAsync (Driver) failed. Message: {Message}", response.Message);
                return (false, response.Message);
            }

            _logger.LogInformation("Driver created successfully: {FullName}", model.FullName);
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("CreateAsync (Driver): Request was cancelled or timed out.");
            return (false, "انتهت مهلة الاتصال بالخادم. حاول مرة أخرى.");
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateAsync (Driver)");
            return (false, ex.InnerException?.Message ?? "حدث خطأ غير متوقع أثناء إنشاء الحساب.");
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Message)> UpdateAsync(int id, EditDriverViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ استخدام PascalCase لتطابق UpdateUserDto في الـ API
            var payload = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                ProfileImage = model.ProfileImage,
                Role = (int)UserRole.Driver,
                LicenseNumber = model.LicenseNumber,
                DriverStatus = (int?)null,
                FactoryId = model.FactoryId,
                IsActive = model.IsActive
            };

            _logger.LogDebug("Updating driver: {DriverId}", id);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}", payload, cts.Token);

            if (response == null)
            {
                _logger.LogWarning("UpdateAsync (Driver): API returned null response for id={Id}", id);
                return (false, "تعذر الاتصال بالخادم. حاول مرة أخرى.");
            }

            if (!response.Success)
            {
                _logger.LogWarning("UpdateAsync (Driver) failed for id={Id}. Message: {Message}", id, response.Message);
                return (false, response.Message);
            }

            _logger.LogInformation("Driver updated successfully: {DriverId}", id);
            return (true, null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UpdateAsync (Driver): Request was cancelled or timed out for id={Id}", id);
            return (false, "انتهت مهلة الاتصال بالخادم. حاول مرة أخرى.");
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in UpdateAsync (Driver) for id={Id}", id);
            return (false, ex.InnerException?.Message ?? "حدث خطأ غير متوقع أثناء التعديل.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting driver: {DriverId}", id);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Users/{id}", cts.Token);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("DeleteAsync (Driver) failed for id={Id}. Message: {Message}", id, response?.Message);
                return false;
            }

            _logger.LogInformation("Driver deleted successfully: {DriverId}", id);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DeleteAsync (Driver): Request was cancelled or timed out for id={Id}", id);
            return false;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in DeleteAsync (Driver) for id={Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(int id, UpdateDriverStatusViewModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ PascalCase لتطابق UpdateDriverStatusDto في الـ API
            var payload = new { DriverStatus = (int)model.DriverStatus };

            _logger.LogDebug("Updating driver status: {DriverId} -> {Status}", id, model.DriverStatus);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}/driver-status", payload, cts.Token);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("UpdateStatusAsync failed for id={Id}. Message: {Message}", id, response?.Message);
                return false;
            }

            _logger.LogInformation("Driver status updated successfully: {DriverId} -> {Status}", id, model.DriverStatus);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UpdateStatusAsync: Request was cancelled or timed out for id={Id}", id);
            return false;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in UpdateStatusAsync for id={Id}", id);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(int Available, int Busy, int Offline)> GetStatusCountsAsync(
        string? search,
        int? factoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            string CountQuery(DriverStatus status) =>
                $"Users?role={(int)UserRole.Driver}&PageNumber=1&PageSize=1&driverStatus={(int)status}"
                + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
                + (factoryId.HasValue ? $"&factoryId={factoryId.Value}" : "");

            // تنفيذ متسلسل — لا نجعل ApiClient يشارك رأس Authorization بين طلبات متزامنة
            var available = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Available), cts.Token);
            var busy = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Busy), cts.Token);
            var offline = await _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Offline), cts.Token);

            return (available, busy, offline);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetStatusCountsAsync: Request was cancelled or timed out.");
            return (0, 0, 0);
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetStatusCountsAsync");
            return (0, 0, 0);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Toggling driver active state: {DriverId}", id);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}/toggle-active", cts.Token);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("ToggleActiveAsync failed for id={Id}. Message: {Message}", id, response?.Message);
                return false;
            }

            // ✅ يعرض حالة السائق الجديدة (true = مفعل، false = معطل)
            var isActive = response.Data is bool active ? active : (response.Message?.Contains("تفعيل") ?? false);
            _logger.LogInformation("Driver active state toggled: {DriverId} -> {IsActive}", id, isActive);
            return isActive;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ToggleActiveAsync: Request was cancelled or timed out for id={Id}", id);
            return false;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ToggleActiveAsync for id={Id}", id);
            return false;
        }
    }
}
