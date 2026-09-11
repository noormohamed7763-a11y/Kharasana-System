using Kharasana.Web.Filters;
using Kharasana.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    [SessionAuthorize("FactoryEmployee")]
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
                TempData[TempDataError] = "تعذر تحميل بيانات المصنع.";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogo(IFormFile logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
            {
                TempData[TempDataError] = "يرجى اختيار صورة صالحة.";
                return RedirectToAction(nameof(Index));
            }

            var logoPath = await _settingsApiService.UploadLogoAsync(logoFile);

            // تحديث الجلسة فوراً ليظهر الشعار الجديد في الـ Navbar
            if (!string.IsNullOrEmpty(logoPath))
            {
                HttpContext.Session.SetString("FactoryLogo", logoPath);
            }

            TempData[string.IsNullOrEmpty(logoPath) ? TempDataError : TempDataSuccess] =
                string.IsNullOrEmpty(logoPath) ? "تعذر رفع الشعار." : "تم تحديث شعار المصنع بنجاح.";

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
                success ? "تم حذف الشعار بنجاح." : "تعذر حذف الشعار.";

            return RedirectToAction(nameof(Index));
        }
    }
}