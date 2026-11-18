using Kharasana.Web.Common;
using Kharasana.Web.Configuration;
using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Settings;
using Kharasana.Web.ViewModels.Factories;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Kharasana.Web.Services.Api
{
    public class SettingsApiService : ISettingsApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<SettingsApiService> _logger;

        public SettingsApiService(
            ApiClient apiClient,
            IOptions<ApiSettings> apiSettings,
            ILogger<SettingsApiService> logger)
        {
            _apiClient = apiClient;
            _apiSettings = apiSettings.Value;
            _logger = logger;
        }

        public async Task<FactorySettingsViewModel?> GetMySettingsAsync()
        {
            var response = await _apiClient.GetAsync<ApiResponse<FactoryDto>>("Settings");

            if (response == null || !response.Success || response.Data == null)
            {
                // كان الرجوع null صامتًا: المستخدم يرى «تعذر تحميل بيانات المصنع» بلا سبب
                // في السجل. رسالة الـ API العربية لا تُعرض (العقد مع المتحكّم null أو كيان)
                // لكنها تُسجَّل الآن.
                _logger.LogWarning(
                    "تعذر تحميل إعدادات المصنع: Success={Success} Data={Data} Message={Message}",
                    response?.Success,
                    response?.Data == null ? "null" : "ok",
                    response?.Message);
                return null;
            }

            var dto = response.Data;

            return new FactorySettingsViewModel
            {
                FactoryId = dto.FactoryId,
                FactoryName = dto.FactoryName,
                OwnerName = dto.OwnerName,
                Phone = dto.Phone,
                Area = dto.Area,
                Logo = LogoFiles.BuildUrl(dto.Logo, _apiSettings),
                IsActive = dto.IsActive
            };
        }

        public async Task<string?> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !LogoFiles.AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("رُفض رفع شعار: امتداد غير مسموح {Extension}", extension);
                return null;
            }

            if (file.Length > LogoFiles.MaxFileSizeInBytes)
            {
                _logger.LogWarning(
                    "رُفض رفع شعار: الحجم {Length} بايت يتجاوز الحد {Max} بايت",
                    file.Length,
                    LogoFiles.MaxFileSizeInBytes);
                return null;
            }

            await using var stream = file.OpenReadStream();

            try
            {
                var response = await _apiClient.PostFileAsync<ApiResponse<object>>(
                    "Settings/logo",
                    stream,
                    file.FileName,
                    "file"
                );

                if (response != null && response.Success && response.Data != null)
                {
                    try
                    {
                        var jsonElement = (JsonElement)response.Data;
                        if (jsonElement.TryGetProperty("logo", out var logoProp))
                        {
                            var logo = logoProp.GetString();
                            return LogoFiles.BuildUrl(logo, _apiSettings);
                        }
                    }
                    catch (Exception ex)
                    {
                        // ✅ مسار بديل مقصود لا سهو: Data قد لا تكون JsonElement إن غيّر
                        //    مُحوِّل التسلسل شكله — فتُقرأ الخاصية بالانعكاس بدل الانفجار.
                        //    لا يُعاد رمي الاستثناء: العقد مع المتحكّم هو null عند الفشل.
                        _logger.LogDebug(ex, "تعذّرت قراءة logo من JsonElement — محاولة بالانعكاس");
                        var logo = response.Data.GetType().GetProperty("logo")?.GetValue(response.Data) as string;
                        if (!string.IsNullOrEmpty(logo))
                            return LogoFiles.BuildUrl(logo, _apiSettings);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "فشل رفع الشعار: Success={Success} Message={Message}",
                        response?.Success,
                        response?.Message);
                }
            }
            catch (Exception ex)
            {
                // كان `catch { }` عارية: تبتلع رسالة الـ API العربية (ومنها 409) فيرى
                // المستخدم فشلًا بلا سبب. القيمة المُعادة تبقى null — العقد مع المتحكّم.
                _logger.LogError(ex, "خطأ في رفع شعار المصنع");
            }

            return null;
        }

        public async Task<bool> DeleteLogoAsync()
        {
            try
            {
                var response = await _apiClient.DeleteAsync<ApiResponse<object>>("Settings/logo");

                if (response == null || !response.Success)
                {
                    _logger.LogWarning(
                        "فشل حذف الشعار: Response={Response} Message={Message}",
                        response == null ? "null" : "Success=false",
                        response?.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف شعار المصنع");
                return false;
            }
        }

        /// <summary>
        /// تشغيل تنظيف ملفات الصور اليتيمة في الخادم المضيف للـ API.
        /// يُرجع عدد الملفات المحذوفة، أو -1 عند الفشل.
        /// </summary>
        public async Task<int> CleanupImagesAsync()
        {
            try
            {
                var response = await _apiClient.PostAsync<ApiResponse<object>>("Settings/cleanup-images", new { });

                if (response == null || !response.Success || response.Data == null)
                {
                    _logger.LogWarning(
                        "فشل تنظيف الصور: Response={Response} Message={Message}",
                        response == null ? "null" : "Success=false",
                        response?.Message);
                    return -1;
                }

                try
                {
                    var jsonElement = (JsonElement)response.Data;
                    if (jsonElement.TryGetProperty("deletedCount", out var countProp))
                        return countProp.GetInt32();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "تعذّرت قراءة deletedCount من JsonElement — محاولة بالانعكاس");
                    var count = response.Data.GetType().GetProperty("deletedCount")
                        ?.GetValue(response.Data);
                    if (count is int deleted)
                        return deleted;
                }

                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تنظيف ملفات الصور اليتيمة");
                return -1;
            }
        }
    }
}