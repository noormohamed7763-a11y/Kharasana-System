using Kharasana.Application.DTOs.Dashboard;

namespace Kharasana.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task<FactoryDashboardDto> GetFactoryDashboardAsync(int factoryId);
}