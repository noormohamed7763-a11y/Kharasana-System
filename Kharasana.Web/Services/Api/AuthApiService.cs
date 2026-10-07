using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Auth;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    public class AuthApiService : IAuthApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<AuthApiService> _logger;

        public AuthApiService(ApiClient apiClient, ILogger<AuthApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<LoginResponseViewModel?> LoginAsync(LoginViewModel model)
        {
            try
            {
                _logger.LogDebug("محاولة تسجيل دخول للمُعرّف {EmailOrPhone}", model.EmailOrPhone);

                var response = await _apiClient.PostAsync<ApiResponse<LoginResponseViewModel>>(
                    "Auth/login",
                    new
                    {
                        EmailOrPhone = model.EmailOrPhone,
                        Password = model.Password
                    });

                if (response == null)
                {
                    _logger.LogWarning("أعادت واجهة الدخول ردًّا فارغًا للمُعرّف {EmailOrPhone}", model.EmailOrPhone);
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("فشل تسجيل الدخول للمُعرّف {EmailOrPhone}. الرسالة: {Message}", model.EmailOrPhone, response.Message);
                    return null;
                }

                if (response.Data == null)
                {
                    _logger.LogWarning("نجح تسجيل الدخول لكن بيانات الردّ فارغة للمُعرّف {EmailOrPhone}", model.EmailOrPhone);
                    return null;
                }

                _logger.LogInformation("نجح تسجيل الدخول للمستخدم {FullName} (المعرّف: {UserId})", response.Data.FullName, response.Data.UserId);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء في LoginAsync للمُعرّف {EmailOrPhone}", model.EmailOrPhone);
                return null;
            }
        }

        public Task LogoutAsync()
        {
            // هنا يمكن إضافة منطق logout إن لزم (نداء API لإبطال التوكن، تسجيل خروج مركزي، الخ)
            _logger.LogDebug("استُدعيت LogoutAsync");
            return Task.CompletedTask;
        }

        public async Task<bool> ActivateAccountAsync(string tokenHash)
        {
            try
            {
                var response = await _apiClient.PostAsync<ApiResponse<object>>(
                    "Auth/activate",
                    tokenHash);

                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استثناء في ActivateAccountAsync");
                return false;
            }
        }
    }
}
