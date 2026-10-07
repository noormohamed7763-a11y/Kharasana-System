using Microsoft.Extensions.Caching.Memory;
using Kharasana.Web.Common;
using Kharasana.Web.Configuration;
using Kharasana.Web.Localization;
using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Factories;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Kharasana.Web.Services.Api
{
    public class FactoryApiService : IFactoryApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ApiSettings _apiSettings;
        private readonly IMemoryCache _cache;

        public FactoryApiService(
            ApiClient apiClient,
            IOptions<ApiSettings> apiSettings,
            IMemoryCache cache)
        {
            _apiClient = apiClient;
            _apiSettings = apiSettings.Value;
            _cache = cache;
        }

        private static FactoryListItemViewModel MapListItem(FactoryDto dto, string? logoUrl) => new()
        {
            FactoryId = dto.FactoryId,
            FactoryName = dto.FactoryName,
            OwnerName = dto.OwnerName,
            Phone = dto.Phone,
            Area = dto.Area,
            Logo = logoUrl,
            IsActive = dto.IsActive,
            HasAccount = dto.HasAccount
        };

        public async Task<ServiceResult<List<FactoryListItemViewModel>>> GetAllAsync()
        {
            return await _cache.GetOrCreateAsync("Factories_All", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                try
                {
                    var response = await _apiClient.GetAsync<ApiResponse<List<FactoryDto>>>("Factories?SortBy=Id&Order=Desc");

                    if (response == null || !response.Success || response.Data == null)
                        return ServiceResult<List<FactoryListItemViewModel>>.Fail(ResponseMessage(response, AppMessages.Common.OperationFailed));

                    return ServiceResult<List<FactoryListItemViewModel>>.Ok(
                        response.Data.Select(d => MapListItem(d, LogoFiles.BuildUrl(d.Logo, _apiSettings))).ToList());
                }
                catch (ApiServiceException) { throw; }
                catch (Exception ex)
                {
                    return ServiceResult<List<FactoryListItemViewModel>>.Fail(ex, AppMessages.Common.OperationFailed);
                }
            }) ?? ServiceResult<List<FactoryListItemViewModel>>.Fail(AppMessages.Common.OperationFailed);
        }

        private void InvalidateCache()
        {
            _cache.Remove("Factories_All");
            _cache.Remove("Factories_Archived");
        }

        public async Task<ServiceResult<List<FactoryListItemViewModel>>> GetArchivedAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<ApiResponse<List<FactoryDto>>>("Factories/archived");

                if (response == null || !response.Success || response.Data == null)
                    return ServiceResult<List<FactoryListItemViewModel>>.Fail(ResponseMessage(response, AppMessages.Common.OperationFailed));

                return ServiceResult<List<FactoryListItemViewModel>>.Ok(
                    response.Data.Select(d => MapListItem(d, LogoFiles.BuildUrl(d.Logo, _apiSettings))).ToList());
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult<List<FactoryListItemViewModel>>.Fail(ex, AppMessages.Common.OperationFailed);
            }
        }

        public async Task<ServiceResult<FactoryDetailsViewModel>> GetByIdAsync(int id)
        {
            try
            {
                var response = await _apiClient.GetAsync<ApiResponse<FactoryDto>>($"Factories/{id}");

                if (response == null || !response.Success || response.Data == null)
                    return ServiceResult<FactoryDetailsViewModel>.Fail(ResponseMessage(response, AppMessages.Common.NotFound));

                var dto = response.Data;

                var details = new FactoryDetailsViewModel
                {
                    FactoryId = dto.FactoryId,
                    FactoryName = dto.FactoryName,
                    OwnerName = dto.OwnerName,
                    Phone = dto.Phone,
                    WhatsApp = dto.WhatsApp,
                    Email = dto.Email,
                    Area = dto.Area,
                    Address = dto.Address,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Logo = LogoFiles.BuildUrl(dto.Logo, _apiSettings),
                    IsActive = dto.IsActive
                };

                return ServiceResult<FactoryDetailsViewModel>.Ok(details);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult<FactoryDetailsViewModel>.Fail(ex, AppMessages.Common.OperationFailed);
            }
        }

        public async Task<ServiceResult> CreateAsync(CreateFactoryViewModel model)
        {
            try
            {
                var response = await _apiClient.PostAsync<ApiResponse<object>>("Factories", model);

                if (response == null || !response.Success)
                    return ServiceResult.Fail(ResponseMessage(response, AppMessages.Error.Created));

                InvalidateCache();
                return ServiceResult.Ok(AppMessages.Success.Created);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ex, AppMessages.Error.Created);
            }
        }

        public async Task<ServiceResult> UpdateAsync(int id, UpdateFactoryViewModel model)
        {
            try
            {
                var updateData = new
                {
                    model.FactoryName,
                    model.OwnerName,
                    model.Phone,
                    model.WhatsApp,
                    model.Email,
                    model.Area,
                    model.Address,
                    model.Latitude,
                    model.Longitude,
                    model.IsActive
                };

                var response = await _apiClient.PutAsync<ApiResponse<object>>($"Factories/{id}", updateData);

                if (response == null || !response.Success)
                    return ServiceResult.Fail(ResponseMessage(response, AppMessages.Error.Updated));

                InvalidateCache();
                _cache.Remove($"Factory_{id}");
                return ServiceResult.Ok(AppMessages.Success.Updated);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ex, AppMessages.Error.Updated);
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            try
            {
                var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Factories/{id}");

                if (response == null || !response.Success)
                    return ServiceResult.Fail(ResponseMessage(response, AppMessages.Error.Archived));

                InvalidateCache();
                _cache.Remove($"Factory_{id}");
                return ServiceResult.Ok(AppMessages.Success.Archived);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ex, AppMessages.Error.Archived);
            }
        }

        public async Task<ServiceResult> RestoreAsync(int id)
        {
            try
            {
                var response = await _apiClient.PostAsync<ApiResponse<object>>($"Factories/restore/{id}", new { });

                if (response == null || !response.Success)
                    return ServiceResult.Fail(ResponseMessage(response, AppMessages.Error.Restored));

                InvalidateCache();
                _cache.Remove($"Factory_{id}");
                return ServiceResult.Ok(AppMessages.Success.Restored);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ex, AppMessages.Error.Restored);
            }
        }

        public async Task<ServiceResult<string>> UploadLogoAsync(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Fail(AppMessages.Error.InvalidLogo);

            // القواعد من LogoFiles لا نسخة محلية: كانت هذه آخر نسخة حرفية متبقّية
            // بعد توحيد FilesController و SettingsApiService في LogoFiles.
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !LogoFiles.AllowedExtensions.Contains(extension))
                return ServiceResult<string>.Fail(AppMessages.Error.InvalidLogo);

            if (file.Length > LogoFiles.MaxFileSizeInBytes)
                return ServiceResult<string>.Fail(AppMessages.Error.LogoUpload);

            try
            {
                await using var stream = file.OpenReadStream();

                var response = await _apiClient.PostFileAsync<ApiResponse<object>>(
                    $"Factories/{id}/logo",
                    stream,
                    file.FileName,
                    "file"
                );

                if (response == null || !response.Success || response.Data == null)
                    return ServiceResult<string>.Fail(ResponseMessage(response, AppMessages.Error.LogoUpload));

                InvalidateCache();
                _cache.Remove($"Factory_{id}");

                var logo = ExtractLogoPath(response.Data);
                return ServiceResult<string>.Ok(LogoFiles.BuildUrl(logo, _apiSettings) ?? string.Empty, AppMessages.Success.LogoUpdated);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult<string>.Fail(ex, AppMessages.Error.LogoUpload);
            }
        }

        public async Task<ServiceResult> DeleteLogoAsync(int id)
        {
            try
            {
                var response = await _apiClient.DeleteAsync<ApiResponse<object>>($"Factories/{id}/logo");

                if (response == null || !response.Success)
                    return ServiceResult.Fail(ResponseMessage(response, AppMessages.Error.LogoDelete));

                InvalidateCache();
                _cache.Remove($"Factory_{id}");
                return ServiceResult.Ok(AppMessages.Success.LogoDeleted);
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                return ServiceResult.Fail(ex, AppMessages.Error.LogoDelete);
            }
        }

        // ============================================================
        // أدوات مساعدة
        // ============================================================

        /// <summary>يعيد رسالة الـ API إن وُجدت، وإلا الرسالة الاحتياطية العربية.</summary>
        private static string ResponseMessage<T>(ApiResponse<T>? response, string fallback)
            => response != null && !string.IsNullOrWhiteSpace(response.Message)
                ? response.Message
                : fallback;

        /// <summary>يستخرج مسار الشعار من حقل <c>Data</c> للاستجابة (JSON أو كائن).</summary>
        private static string? ExtractLogoPath(object? data)
        {
            if (data == null)
                return null;

            if (data is JsonElement jsonElement && jsonElement.TryGetProperty("logo", out var logoProp))
                return logoProp.GetString();

            var prop = data.GetType().GetProperty("logo");
            return prop?.GetValue(data) as string;
        }
    }
}