using Kharasana.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kharasana.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IWhatsAppService whatsAppService, IConfiguration config, ILogger<NotificationService> logger)
    {
        _whatsAppService = whatsAppService;
        _config = config;
        _logger = logger;
    }

    public async Task SendHawalaOverdueAlertAsync(string managerPhone, string orderNumber, decimal amount)
    {
        var template = _config["WhatsAppTemplates:HawalaOverdue"];
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning("WhatsApp template 'HawalaOverdue' is missing in configuration.");
            return;
        }
        await _whatsAppService.SendTemplateMessageAsync(managerPhone, template, orderNumber, amount.ToString("N0"));
    }

    public async Task SendLowStockAlertAsync(string managerPhone, string materialName, decimal currentStock, decimal minStock)
    {
        var template = _config["WhatsAppTemplates:LowStock"];
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning("WhatsApp template 'LowStock' is missing in configuration.");
            return;
        }
        await _whatsAppService.SendTemplateMessageAsync(managerPhone, template, materialName, currentStock.ToString("N0"), minStock.ToString("N0"));
    }

    public async Task SendProductionBatchStatusAlertAsync(string managerPhone, string batchNumber, string newStatus)
    {
        var template = _config["WhatsAppTemplates:ProductionBatchStatus"];
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning("WhatsApp template 'ProductionBatchStatus' is missing in configuration.");
            return;
        }
        await _whatsAppService.SendTemplateMessageAsync(managerPhone, template, batchNumber, newStatus);
    }
}
