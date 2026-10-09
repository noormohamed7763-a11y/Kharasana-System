using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
                // الخدمة ترمي استثناءً إذا فشل تسجيل الدخول
                var result = await _authService.LoginAsync(model);

                // تفريغ الجلسة قبل كتابة الهوية الجديدة إلزامي
                HttpContext.Session.Clear();
                HttpContext.Session.SetString("Token", result!.Token);

                // إصدار المطالبات (Claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result!.FullName),
                    new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new Claim(ClaimTypes.Role, result.Role)
                };

                if (result.FactoryId.HasValue)
                {
                    claims.Add(new Claim("FactoryId", result.FactoryId.Value.ToString()));
                }

                if (result.FactoryIsActive.HasValue)
                {
                    claims.Add(new Claim("FactoryIsActive", result.FactoryIsActive.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync("WebCookie", new ClaimsPrincipal(claimsIdentity), authProperties);

                if (result.FactoryId.HasValue)
                {
                    await CacheFactoryInfoAsync();
                }

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
                // نبقى في صفحة الدخول ونعرض الرسالة الموحدة
                ModelState.AddModelError(string.Empty, ex.Error.FormatMessage(ex.MessageArgs ?? Array.Empty<object>()));
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
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            await HttpContext.SignOutAsync("WebCookie");
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Activate(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("التوكن مفقود.");

            // عرض نموذج تعيين كلمة المرور للمستخدم
            return View(new ActivateAccountViewModel { TokenHash = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(ActivateAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "كلمتا المرور غير متطابقتين.");
                return View(model);
            }

            var success = await _authService.ActivateAccountAsync(model.TokenHash, model.NewPassword);
            if (success)
            {
                ViewBag.Message = "تم تفعيل الحساب وتعيين كلمة المرور بنجاح، يمكنك الآن تسجيل الدخول.";
                return View("ActivateSuccess");
            }

            ViewBag.Message = "فشل تفعيل الحساب، الرابط قد يكون غير صالح أو منتهي الصلاحية.";
            return View("ActivateError");
        }
    }
}