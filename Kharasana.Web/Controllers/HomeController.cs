using System.Diagnostics;
using Kharasana.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.Web.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (IsLoggedIn)
                return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// صفحة الخطأ الموحّدة:
        ///   - 500 ← من UseExceptionHandler عند أي استثناء غير معالَج
        ///   - 403/404 ← من UseStatusCodePagesWithReExecute عبر ?statusCode=
        /// تُسجَّل التفاصيل هنا مع الرقم المرجعي نفسه الذي يعرضه للمستخدم،
        /// ليتسنى تتبّع الخطأ في السجلات الملفّية.
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // 404/403 صفحة جمالية بسيطة بلا إزعاج الدعم
            if (statusCode == 404 || statusCode == 403)
            {
                _logger.LogInformation(
                    "Page not found or forbidden. StatusCode={StatusCode} RequestId={RequestId} Path={Path}",
                    statusCode, requestId, HttpContext.Request.Path);

                return View(new ErrorViewModel
                {
                    RequestId = requestId,
                    StatusCode = statusCode
                });
            }

            // 500 (أو أي خطأ آخر): نسجّل الخطأ مع رقمه المرجعي ليتسنى ربط الشاشة بالسجل
            _logger.LogError(
                "Unhandled exception rendered the Error page. RequestId={RequestId} Path={Path}",
                requestId, HttpContext.Request.Path);

            return View(new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = statusCode
            });
        }
    }
}