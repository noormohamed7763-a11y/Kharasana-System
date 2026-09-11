using Kharasana.Web.Models.Dashboard;

namespace Kharasana.Web.Services.Interfaces
{
    public interface IDashboardApiService
    {
        Task<DashboardViewModel?> GetAdminDashboardAsync();

        Task<DashboardViewModel?> GetFactoryDashboardAsync(int factoryId);
    }
}