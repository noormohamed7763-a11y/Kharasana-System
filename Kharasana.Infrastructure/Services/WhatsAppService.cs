using Kharasana.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Kharasana.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendTemplateMessageAsync(string phone, string templateName, params string[] parameters)
    {
        var settings = _config.GetSection("WhatsAppSettings");
        var baseUrl = settings["BaseUrl"];
        var phoneNumberId = settings["PhoneNumberId"];
        var accessToken = settings["AccessToken"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("WhatsApp settings are missing.");
            return false;
        }

        var url = $"{baseUrl}{phoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "ar" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new { type = "text", text = p })
                    }
                }
            }
        };

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("WhatsApp message sent successfully to {Phone}", phone);
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send WhatsApp message. Error: {Error}", errorContent);
            return false;
        }
    }
}
