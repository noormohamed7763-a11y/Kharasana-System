using Kharasana.Application.Common;
using Kharasana.Domain.Enums;
using Kharasana.Web.Filters;
using Kharasana.Web.ViewModels.Dashboard;
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

            if (RoleValue == UserRole.Admin)
            {
                vm = await _dashboardApiService.GetAdminDashboardAsync();
            }
            else if (RoleValue == UserRole.FactoryEmployee)
            {
                if (!FactoryId.HasValue)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                vm = await _dashboardApiService.GetFactoryDashboardAsync(FactoryId.Value);
            }

            vm ??= new DashboardViewModel();

            return View(vm);
        }

        // بلا async: لا await في الجسم — كان المعدِّل مضلِّلًا، وCS1998 لم يعد يُنبِّه عليه
        // (أُزيل من Roslyn، واستُبدل بالمحلّل الاختياري IDE0390) فبقي صامتًا.
        public IActionResult Factory()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}