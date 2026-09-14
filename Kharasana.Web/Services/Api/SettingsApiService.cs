using Kharasana.Web.Configuration;
using Kharasana.Application.Common;
using Kharasana.Web.Models.Settings;
using Kharasana.Web.ViewModels.Factories;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Kharasana.Web.Services.Api
{
    public class SettingsApiService : ISettingsApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ApiSettings _apiSettings;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        public SettingsApiService(ApiClient apiClient, IOptions<ApiSettings> apiSettings)
        {
            _apiClient = apiClient;
            _apiSettings = apiSettings.Value;
        }

        private string? BuildLogoUrl(string? logo)
        {
            if (string.IsNullOrWhiteSpace(logo))
                return null;

            if (logo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return logo;

            if (!logo.StartsWith("/", StringComparison.Ordinal))
                return null;

            // الآن نخدم الملفات عبر FilesController على نفس أصل الويب
            return $"/Files/factories{logo}";
        }

        public async Task<FactorySettingsViewModel?> GetMySettingsAsync()
        {
            var response = await _apiClient.GetAsync<ApiResponse<FactoryDto>>("Settings");

            if (response == null || !response.Success || response.Data == null)
                return null;

            var dto = response.Data;

            return new FactorySettingsViewModel
            {
                FactoryId = dto.FactoryId,
                FactoryName = dto.FactoryName,
                OwnerName = dto.OwnerName,
                Phone = dto.Phone,
                Area = dto.Area,
                Logo = BuildLogoUrl(dto.Logo),
                IsActive = dto.IsActive
            };
        }

        public async Task<string?> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return null;

            if (file.Length > MaxFileSizeInBytes)
                return null;

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
                            return BuildLogoUrl(logo);
                        }
                    }
                    catch
                    {
                        var logo = response.Data.GetType().GetProperty("logo")?.GetValue(response.Data) as string;
                        if (!string.IsNullOrEmpty(logo))
                            return BuildLogoUrl(logo);
                    }
                }
            }
            catch
            {
                // في حالة حدوث خطأ، نعيد null
            }

            return null;
        }

        public async Task<bool> DeleteLogoAsync()
        {
            try
            {
                var response = await _apiClient.DeleteAsync<ApiResponse<object>>("Settings/logo");
                return response != null && response.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}