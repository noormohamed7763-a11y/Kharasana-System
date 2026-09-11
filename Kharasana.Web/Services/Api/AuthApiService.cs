using System;
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
                _logger.LogDebug("Attempting login for {EmailOrPhone}", model.EmailOrPhone);

                var response = await _apiClient.PostAsync<ApiResponse<LoginResponseViewModel>>(
                    "Auth/login",
                    new
                    {
                        EmailOrPhone = model.EmailOrPhone,
                        Password = model.Password
                    });

                if (response == null)
                {
                    _logger.LogWarning("Login API returned null for {EmailOrPhone}", model.EmailOrPhone);
                    return null;
                }

                if (!response.Success)
                {
                    _logger.LogWarning("Login failed for {EmailOrPhone}. Message: {Message}", model.EmailOrPhone, response.Message);
                    return null;
                }

                if (response.Data == null)
                {
                    _logger.LogWarning("Login succeeded but response.Data is null for {EmailOrPhone}", model.EmailOrPhone);
                    return null;
                }

                _logger.LogInformation("Login successful for user {FullName} (id: {UserId})", response.Data.FullName, response.Data.UserId);
                return response.Data;
            }
            catch (ApiServiceException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during LoginAsync for {EmailOrPhone}", model.EmailOrPhone);
                return null;
            }
        }

        public Task LogoutAsync()
        {
            // هنا يمكن إضافة منطق logout إن لزم (نداء API لإبطال التوكن، تسجيل خروج مركزي، الخ)
            _logger.LogDebug("LogoutAsync called");
            return Task.CompletedTask;
        }
    }
}
