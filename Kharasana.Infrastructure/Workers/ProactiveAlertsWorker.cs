using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kharasana.Infrastructure.Workers;

public class ProactiveAlertsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProactiveAlertsWorker> _logger;

    public ProactiveAlertsWorker(IServiceProvider serviceProvider, ILogger<ProactiveAlertsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProactiveAlertsWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // 1. Alert: Overdue Hawala (Pending > 24 hours)
                await CheckOverdueHawalaAsync(unitOfWork, notificationService);

                // 2. Alert: Low Stock
                await CheckLowStockAsync(unitOfWork, notificationService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing proactive alerts.");
            }

            // Wait 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task CheckOverdueHawalaAsync(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-24);
        var overduePayments = await unitOfWork.OrderPayments.GetOverdueVerificationsAsync(cutoffDate);

        foreach (var payment in overduePayments)
        {
            if (payment.Order?.Factory?.WhatsApp is not null)
            {
                try
                {
                    await notificationService.SendHawalaOverdueAlertAsync(
                        payment.Order.Factory.WhatsApp,
                        payment.Order.OrderNumber,
                        payment.Amount);
                    _logger.LogInformation("Hawala overdue alert sent for payment {PaymentId}", payment.OrderPaymentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send Hawala overdue alert for payment {PaymentId}", payment.OrderPaymentId);
                }
            }
        }
    }

    private async Task CheckLowStockAsync(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        var lowStockMaterials = await unitOfWork.RawMaterials.GetLowStockAsync();

        foreach (var material in lowStockMaterials)
        {
            if (material.Factory?.WhatsApp is not null)
            {
                try
                {
                    await notificationService.SendLowStockAlertAsync(
                        material.Factory.WhatsApp,
                        material.Name,
                        material.CurrentStock,
                        material.MinStockLevel);
                    _logger.LogInformation("Low stock alert sent for material {MaterialId}", material.RawMaterialId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send low stock alert for material {MaterialId}", material.RawMaterialId);
                }
            }
        }
    }
}