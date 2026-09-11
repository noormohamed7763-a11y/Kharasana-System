using Kharasana.Web.Filters;
using Kharasana.Web.Models.Dashboard;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [SessionAuthorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardApiService _dashboardApiService;

        public DashboardController(IDashboardApiService dashboardApiService)
        {
            _dashboardApiService = dashboardApiService;
        }

        public async Task<IActionResult> Index()
        {
            DashboardViewModel? vm = null;

            if (Role == "Admin")
            {
                vm = await _dashboardApiService.GetAdminDashboardAsync();
            }
            else if (Role == "FactoryEmployee")
            {
                if (FactoryId.HasValue)
                {
                    vm = await _dashboardApiService.GetFactoryDashboardAsync(FactoryId.Value);
                }
            }

            vm ??= new DashboardViewModel();

            return View(vm);
        }
    }
}