using Kharasana.Web.Filters;
using Kharasana.Web.Localization;
using Kharasana.Web.Services.Api;
using Kharasana.Web.Models;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Auth;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kharasana.Web.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthApiService _authService;
        private readonly ISettingsApiService _settingsApiService;

        public AccountController(IAuthApiService authService, ISettingsApiService settingsApiService)
        {
            _authService = authService;
            _settingsApiService = settingsApiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (IsLoggedIn)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        /// <summary>
        /// صفحة منع الوصول (403). <b>لا يناظرها تكوين ASP.NET Authorization</b>: هذا
        /// المشروع لا يسجّل أي AuthenticationScheme (انظر Program.cs) والحراسة كلها عبر
        /// <see cref="SessionAuthorizeAttribute"/>. الفعل باقٍ كصفحة 403 صريحة، ويُحوَّل
        /// إليه من <c>DashboardController.Index</c> حين يكون موظف مصنعٍ بلا مصنعٍ مُسنَد
        /// في جلسته (وإلا عرضت اللوحة بيانات بلا مصنع).
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("Error", new ErrorViewModel { StatusCode = 403 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _authService.LoginAsync(model);

                if (result == null)
                {
                    ModelState.AddModelError("", AppMessages.Error.InvalidLogin);
                    return View(model);
                }

                // ✅ تفريغ الجلسة قبل كتابة الهوية الجديدة إلزامي: معرّف الجلسة (SessionId)
                //    لا يُدوَّر في ASP.NET Core، فأي مفتاح متبقٍّ من حسابٍ سابق ينتقل إلى
                //    الحساب الجديد. الأثر الفعلي: مديرٌ يسجّل الدخول بعد موظف مصنع كان
                //    يحمل FactoryId قديمًا — فيُقيَّد في تعيين السائقين (OrderWorkflow)
                //    ويُعرض له اسم مصنعٍ وشعاره ليسا مصنعه.
                //    TempData لا يتأثر: مزوّده في هذا المشروع كوكي لا جلسة (انظر Program.cs).
                HttpContext.Session.Clear();

                HttpContext.Session.SetString("Token", result.Token);
                HttpContext.Session.SetString("FullName", result.FullName);
                HttpContext.Session.SetString("Role", result.Role);

                if (result.FactoryId.HasValue)
                {
                    HttpContext.Session.SetInt32("FactoryId", result.FactoryId.Value);

                    // تحميل اسم المصنع وشعاره مرة واحدة عند الدخول وتخزينهما في الجلسة
                    // ليعرض الـ Navbar الشعار الدائري (مع بديل حرفي عند غيابه).
                    await CacheFactoryInfoAsync();
                }

                // ✅ تخزين حالة المصنع في الجلسة لاستخدامها في الواجهة
                // (إخفاء الإجراءات غير المسموحة + بانر دائم عند التعطيل) — بلا نداء API إضافي.
                if (result.FactoryIsActive.HasValue)
                {
                    HttpContext.Session.SetInt32("FactoryIsActive", result.FactoryIsActive.Value ? 1 : 0);
                }
                else
                {
                    HttpContext.Session.Remove("FactoryIsActive");
                }

                // ✅ تحذير بعد تسجيل الدخول (مثل: المصنع غير نشط) — يُعرض عبر _Alerts في الصفحة التالية
                if (!string.IsNullOrWhiteSpace(result.Notification))
                    TempData[TempDataWarning] = result.Notification;

                if (result.Role == UserRole.FactoryEmployee.ToString())
                {
                    return RedirectToAction("Factory", "Dashboard");
                }

                return RedirectToAction("Index", "Dashboard");
            }
            catch (ApiServiceException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// جلب بيانات المصنع الحالية (الاسم + الشعار برابط كامل) وتخزينها في الجلسة.
        /// عند أي فشل يبقى البديل الحرفي ظاهراً في الـ Navbar ولا يُمنع تسجيل الدخول.
        /// </summary>
        private async Task CacheFactoryInfoAsync()
        {
            try
            {
                var settings = await _settingsApiService.GetMySettingsAsync();
                if (settings == null)
                    return;

                HttpContext.Session.SetString("FactoryName", settings.FactoryName ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(settings.Logo))
                    HttpContext.Session.SetString("FactoryLogo", settings.Logo);
            }
            catch
            {
                // تجاهل: لا نمنع تسجيل الدخول بسبب فشل جلب الشعار
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();

            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
    }
}