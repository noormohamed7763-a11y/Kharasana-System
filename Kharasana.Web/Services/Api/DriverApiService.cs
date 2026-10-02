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

    // ✅ مهلة الطلب (بالثواني) — 10 ثوانٍ كافية لمعظم العمليات، و10 ثوانٍ أسرع من 30 ثانية في حالة الخطأ
    private const int RequestTimeoutSeconds = 10;

    public DriverApiService(ApiClient apiClient, ILogger<DriverApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// ينفّذ طلب API مع مهلة زمنية، ويوحّد المعالجة المركّزة للأخطاء لكل الدوال.
    /// يحوّل انتهاء المهلة إلى ApiServiceException (كما يفعل ApiClient) —
    /// يلتقط الإلغاء الخارجي (غادر المستخدم الصفحة) ويعيد القيمة الافتراضية بدلاً من رمي استثناء.
    /// </summary>
    private async Task<T?> ExecuteAsync<T>(
        string operation,
        string detail,
        CancellationToken cancellationToken,
        T? cancellationFallback,
        Func<Exception, T?> errorFallback,
        Func<CancellationToken, Task<T?>> action)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

        try
        {
            return await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{Operation}: أُلغي الطلب أو انتهت مهلته. {Detail}", operation, detail);
            return cancellationFallback;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء في {Operation}. {Detail}", operation, detail);
            return errorFallback(ex);
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<DriverListItemViewModel>?> GetDriversAsync(
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
            cancellationFallback: null,
            errorFallback: _ => null,
            action: async token =>
            {
                // ✅ بناء رابط الطلب مع الترتيب الافتراضي (الأحدث أولاً)
                var query = $"Users?role={(int)UserRole.Driver}&PageNumber={pageNumber}&PageSize={pageSize}&SortBy=Id&Order=Desc";

                if (!string.IsNullOrWhiteSpace(search))
                    query += $"&Search={Uri.EscapeDataString(search)}";

                if (factoryId.HasValue)
                    query += $"&factoryId={factoryId.Value}";

                _logger.LogDebug("جلب السائقين — الاستعلام: {Query}", query);

                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(query, token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetDriversAsync: ردّ الـ API فارغ أو فاشل للاستعلام {Query}", query);
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
            });
    }

    /// <inheritdoc />
    public async Task<DriverListItemViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetByIdAsync),
            $"driverId={id}",
            cancellationToken,
            cancellationFallback: null,
            errorFallback: _ => null,
            action: async token =>
            {
                var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{id}", token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetByIdAsync: تعذّر جلب السائق {DriverId}", id);
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
            });
    }

    /// <inheritdoc />
    public async Task<EditDriverViewModel?> GetForEditAsync(int driverId, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            nameof(GetForEditAsync),
            $"driverId={driverId}",
            cancellationToken,
            cancellationFallback: null,
            errorFallback: _ => null,
            action: async token =>
            {
                var response = await _apiClient.GetAsync<ApiResponse<UserDto>>($"Users/{driverId}", token);

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning("GetForEditAsync: تعذّر جلب السائق {DriverId}", driverId);
                    return null;
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

    /// <inheritdoc />
    public async Task<(bool Success, string? Message)> CreateAsync(CreateDriverViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync<(bool Success, string? Message)>(
            "CreateAsync (Driver)",
            model.FullName ?? string.Empty,
            cancellationToken,
            cancellationFallback: (false, "انتهت مهلة الاتصال بالخادم. حاول مرة أخرى."),
            errorFallback: ex => (false, "حدث خطأ غير متوقع أثناء إنشاء الحساب. حاول مرة أخرى."),
            action: async token =>
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

                _logger.LogDebug("إنشاء سائق جديد: {FullName}", model.FullName);

                var response = await _apiClient.PostAsync<ApiResponse<object>>("Users", payload, token);

                if (response == null)
                {
                    _logger.LogWarning("CreateAsync (سائق): ردّ الـ API فارغ.");
                    return (false, "تعذر الاتصال بالخادم. حاول مرة أخرى.");
                }

                if (!response.Success)
                {
                    _logger.LogWarning("فشل CreateAsync (سائق). الرسالة: {Message}", response.Message);
                    return (false, response.Message);
                }

                _logger.LogInformation("أُنشئ السائق: {FullName}", model.FullName);
                return (true, null);
            });

        return result;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Message)> UpdateAsync(int id, EditDriverViewModel model, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync<(bool Success, string? Message)>(
            "UpdateAsync (Driver)",
            $"driverId={id}",
            cancellationToken,
            cancellationFallback: (false, "انتهت مهلة الاتصال بالخادم. حاول مرة أخرى."),
            errorFallback: ex => (false, "حدث خطأ غير متوقع أثناء التعديل. حاول مرة أخرى."),
            action: async token =>
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
                    FactoryId = model.FactoryId,
                    IsActive = model.IsActive
                };

                _logger.LogDebug("تعديل السائق: {DriverId}", id);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}", payload, token);

                if (response == null)
                {
                    _logger.LogWarning("UpdateAsync (سائق): ردّ الـ API فارغ للمعرّف {Id}", id);
                    return (false, "تعذر الاتصال بالخادم. حاول مرة أخرى.");
                }

                if (!response.Success)
                {
                    _logger.LogWarning("فشل UpdateAsync (سائق) للمعرّف {Id}. الرسالة: {Message}", id, response.Message);
                    return (false, response.Message);
                }

                _logger.LogInformation("عُدّل السائق: {DriverId}", id);
                return (true, null);
            });

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            "DeleteAsync (Driver)",
            $"driverId={id}",
            cancellationToken,
            cancellationFallback: false,
            errorFallback: _ => false,
            action: async token =>
            {
                _logger.LogDebug("حذف السائق: {DriverId}", id);

                var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Users/{id}", token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل DeleteAsync (سائق) للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    return false;
                }

                _logger.LogInformation("حُذف السائق: {DriverId}", id);
                return true;
            });
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(int id, UpdateDriverStatusViewModel model, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            "UpdateStatusAsync",
            $"driverId={id} status={model.DriverStatus}",
            cancellationToken,
            cancellationFallback: false,
            errorFallback: _ => false,
            action: async token =>
            {
                // ✅ PascalCase لتطابق UpdateDriverStatusDto في الـ API
                var payload = new { DriverStatus = (int)model.DriverStatus };

                _logger.LogDebug("تحديث حالة السائق: {DriverId} ← {Status}", id, model.DriverStatus);

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Users/{id}/driver-status", payload, token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل UpdateStatusAsync للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    return false;
                }

                _logger.LogInformation("حُدّثت حالة السائق: {DriverId} ← {Status}", id, model.DriverStatus);
                return true;
            });
    }

    /// <inheritdoc />
    public async Task<(int Available, int Busy, int Offline)> GetStatusCountsAsync(
        string? search,
        int? factoryId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<(int Available, int Busy, int Offline)>(
            nameof(GetStatusCountsAsync),
            "",
            cancellationToken,
            cancellationFallback: (Available: 0, Busy: 0, Offline: 0),
            errorFallback: _ => (Available: 0, Busy: 0, Offline: 0),
            action: async token =>
            {
                string CountQuery(DriverStatus status) =>
                    $"Users?role={(int)UserRole.Driver}&PageNumber=1&PageSize=1&driverStatus={(int)status}"
                    + (string.IsNullOrWhiteSpace(search) ? "" : $"&Search={Uri.EscapeDataString(search)}")
                    + (factoryId.HasValue ? $"&factoryId={factoryId.Value}" : "");

                // ✅ تنفيذ متوازي — 3x أسرع من التنفيذ المتتالي
                // ApiClient يضيف رأس Authorization على HttpRequestMessage نفسه في SendAsync،
                // لا على DefaultRequestHeaders — فلا مخاطرة بين الطلبات المتزامنة
                var availableTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Available), token);
                var busyTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Busy), token);
                var offlineTask = _apiClient.GetPagedTotalAsync<UserDto>(CountQuery(DriverStatus.Offline), token);

                await Task.WhenAll(availableTask, busyTask, offlineTask);

                return (Available: availableTask.Result, Busy: busyTask.Result, Offline: offlineTask.Result);
            });
    }

    /// <inheritdoc />
    public async Task<bool> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            "ToggleActiveAsync",
            $"driverId={id}",
            cancellationToken,
            cancellationFallback: false,
            errorFallback: _ => false,
            action: async token =>
            {
                _logger.LogDebug("تبديل تفعيل حساب السائق: {DriverId}", id);

                // ✅ القيمة المنطقية بِنيتها المعلنة لا عبر object: القراءة إلى
                //    ApiResponse<object> تُنتج JsonElement دائماً (كما يعالجه
                //    FactoryApiService و SettingsApiService)، فكان فحص `Data is bool`
                //    لا يصدُق أبداً ويسقط التنفيذ إلى تخمين من نصّ الرسالة العربية
                //    («تفعيل») — أي أن عرض «تم تفعيل» أو «تم إيقاف» كان رهين صياغة
                //    رسالة في طبقة Application.
                var response = await _apiClient.PutAsync<ApiResponse<bool>>($"Users/{id}/toggle-active", token);

                if (response == null || !response.Success)
                {
                    _logger.LogWarning("فشل ToggleActiveAsync للمعرّف {Id}. الرسالة: {Message}", id, response?.Message);
                    return false;
                }

                var isActive = response.Data;
                _logger.LogInformation("بُدّل تفعيل حساب السائق: {DriverId} ← {IsActive}", id, isActive);
                return isActive;
            });
    }
}
