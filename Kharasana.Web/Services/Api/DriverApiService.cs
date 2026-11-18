using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Web.ViewModels.Drivers;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Kharasana.Web.Services.Api;

/// <summary>
/// خدمة السائقين - تنفيذ واجهة IDriverApiService
/// </summary>
public class DriverApiService : IDriverApiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<DriverApiService> _logger;

    private const int RequestTimeoutSeconds = 10;

    public DriverApiService(ApiClient apiClient, ILogger<DriverApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    private async Task<T> ExecuteAsync<T>(
        string operation,
        string detail,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<T>> action)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

        try
        {
            return await action(cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("{Operation}: أُلغي الطلب أو انتهت مهلته. {Detail}", operation, detail);
            throw new ApiServiceException(HttpStatusCode.RequestTimeout, ApiErrorCatalog.NetworkError, innerException: ex);
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء غير متوقع في {Operation}. {Detail}", operation, detail);
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
        }
    }

    public async Task<PagedResult<DriverListItemViewModel>> GetDriversAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetDriversAsync),
            $"pageNumber={pageNumber} pageSize={pageSize}",
            cancellationToken,
            async token =>
            {
                var query = $"Users?role={(int)UserRole.Driver}&PageNumber={pageNumber}&PageSize={pageSize}&SortBy=Id&Order=Desc";
                if (!string.IsNullOrWhiteSpace(search)) query += $"&Search={Uri.EscapeDataString(search)}";
                if (factoryId.HasValue) query += $"&factoryId={factoryId.Value}";

                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(query, token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetDriversAsync: ردّ الـ API فارغ أو فاشل للاستعلام {Query}", query);
                    throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError);
                }

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
                    }).ToList()
                };
            });
    }

    public async Task<DriverListItemViewModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetByIdAsync),
            $"driverId={id}",
            cancellationToken,
            async token =>
            {
                var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{id}", token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetByIdAsync: تعذّر جلب السائق {DriverId}", id);
                    throw new ApiServiceException(HttpStatusCode.NotFound, ApiErrorCatalog.DriverNotFound, new object[] { id });
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
            });
    }

    public async Task<EditDriverViewModel> GetForEditAsync(int driverId, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetForEditAsync),
            $"driverId={driverId}",
            cancellationToken,
            async token =>
            {
                var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{driverId}", token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetForEditAsync: تعذّر جلب السائق {DriverId}", driverId);
                    throw new ApiServiceException(HttpStatusCode.NotFound, ApiErrorCatalog.DriverNotFound, new object[] { driverId });
                }

                var u = response.Data;
                return new EditDriverViewModel
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    Phone = u.Phone,
                    LicenseNumber = u.LicenseNumber,
                    ProfileImage = u.ProfileImage,
                    FactoryId = u.FactoryId,
                    IsActive = u.IsActive
                };
            });
    }

    public async Task CreateAsync(CreateDriverViewModel model, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            "CreateAsync (Driver)",
            model.FullName ?? string.Empty,
            cancellationToken,
            async token =>
            {
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

                var response = await _apiClient.PostAsync<ApiResponse<object>>("Users", payload, token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل CreateAsync (سائق). الرسالة: {Message}", response?.Message);
                    throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.DriverCreateFailed, new object[] { response?.Message ?? "سبب غير معروف" });
                }

                return Task.CompletedTask;
            });
    }

    public async Task UpdateAsync(int id, EditDriverViewModel model, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            "UpdateAsync (Driver)",
            $"driverId={id}",
            cancellationToken,
            async token =>
            {
                var payload = new
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    ProfileImage = model.ProfileImage,
                    Role = (int)UserRole.Driver,
                    LicenseNumber = model.LicenseNumber,
                    FactoryId = model.FactoryId,
                    IsActive = model.IsActive
                };

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}", payload, token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل UpdateAsync (سائق) للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.DriverUpdateFailed, new object[] { id, response?.Message ?? "سبب غير معروف" });
                }
                return Task.CompletedTask;
            });
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            "DeleteAsync (Driver)",
            $"driverId={id}",
            cancellationToken,
            async token =>
            {
                var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Users/{id}", token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل DeleteAsync (سائق) للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.DriverDeleteFailed, new object[] { id, response?.Message ?? "سبب غير معروف" });
                }
                return Task.CompletedTask;
            });
    }

    public async Task UpdateStatusAsync(int id, UpdateDriverStatusViewModel model, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            "UpdateStatusAsync",
            $"driverId={id} status={model.DriverStatus}",
            cancellationToken,
            async token =>
            {
                var payload = new { DriverStatus = (int)model.DriverStatus };
                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}/driver-status", payload, token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل UpdateStatusAsync للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ServerError);
                }
                return Task.CompletedTask;
            });
    }

    public async Task<(int Available, int Busy, int Offline)> GetStatusCountsAsync(
        string? search,
        int? factoryId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetStatusCountsAsync),
            "",
            cancellationToken,
            async token =>
            {
                string CountQuery(DriverStatus status) =>
                    $"Users?role={(int)UserRole.Driver}&PageNumber=1&PageSize=1&driverStatus={(int)status}"
                    + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
                    + (factoryId.HasValue ? $"&factoryId={factoryId.Value}" : "");

                var availableTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Available), token);
                var busyTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Busy), token);
                var offlineTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Offline), token);

                await Task.WhenAll(availableTask, busyTask, offlineTask);

                return (Available: availableTask.Result, Busy: busyTask.Result, Offline: offlineTask.Result);
            });
    }

    public async Task<bool> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            "ToggleActiveAsync",
            $"driverId={id}",
            cancellationToken,
            async token =>
            {
                var response = await _apiClient.PutAsync<ApiResponse<bool>>($"Users/{id}/toggle-active", token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل ToggleActiveAsync للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    throw new ApiServiceException(HttpStatusCode.BadRequest, ApiErrorCatalog.ServerError);
                }

                return response.Data;
            });
    }
}