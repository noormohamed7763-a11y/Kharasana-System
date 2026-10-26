namespace Kharasana.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendHawalaOverdueAlertAsync(string managerPhone, string orderNumber, decimal amount);
    Task SendLowStockAlertAsync(string managerPhone, string materialName, decimal currentStock, decimal minStock);
    Task SendProductionBatchStatusAlertAsync(string managerPhone, string batchNumber, string newStatus);
}
