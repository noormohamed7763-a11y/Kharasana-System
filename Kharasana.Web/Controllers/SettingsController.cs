using Kharasana.Application.Common;
using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [SessionAuthorize(Roles.FactoryEmployee)]
    public class SettingsController : BaseController
    {
        private readonly ISettingsApiService _settingsApiService;

        public SettingsController(ISettingsApiService settingsApiService)
        {
            _settingsApiService = settingsApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _settingsApiService.GetMySettingsAsync();

            if (model == null)
            {
                TempData[TempDataError] = AppMessages.Error.FactorySettingsLoad;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogo(IFormFile logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
            {
                TempData[TempDataError] = AppMessages.Error.InvalidLogo;
                return RedirectToAction(nameof(Index));
            }

            var logoPath = await _settingsApiService.UploadLogoAsync(logoFile);
            var uploaded = !string.IsNullOrEmpty(logoPath);

            // تحديث الجلسة فوراً ليظهر الشعار الجديد في الـ Navbar
            if (uploaded)
            {
                HttpContext.Session.SetString("FactoryLogo", logoPath!);
            }

            TempData[uploaded ? TempDataSuccess : TempDataError] =
                uploaded ? AppMessages.Success.FactoryLogoUpdated : AppMessages.Error.LogoUploadShort;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLogo()
        {
            var success = await _settingsApiService.DeleteLogoAsync();

            // إزالة الشعار من الجلسة ليعود البديل الحرفي في الـ Navbar
            if (success)
            {
                HttpContext.Session.Remove("FactoryLogo");
            }

            TempData[success ? TempDataSuccess : TempDataError] =
                success ? AppMessages.Success.LogoDeleted : AppMessages.Error.LogoDelete;

            return RedirectToAction(nameof(Index));
        }
    }
}