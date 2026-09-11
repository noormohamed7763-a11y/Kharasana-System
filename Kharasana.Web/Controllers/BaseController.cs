using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// مفاتيح TempData الموحّدة للرسائل. تُقرأ كلها من الجزء الموحّد
        /// Components/_Alerts في كل الصفحات — تُفضَّل على الـ strings العشوائية.
        /// </summary>
        public const string TempDataSuccess = "Success";
        public const string TempDataError = "Error";
        public const string TempDataInfo = "Info";
        public const string TempDataWarning = "Warning";

        protected string? Token =>
            HttpContext.Session.GetString("Token");

        protected string? FullName =>
            HttpContext.Session.GetString("FullName");

        protected string? Role =>
            HttpContext.Session.GetString("Role");

        protected int? FactoryId =>
            HttpContext.Session.GetInt32("FactoryId");

        /// <summary>
        /// حالة المصنع (نشط/موقوف) من الجلسة — null للحسابات غير المرتبطة بمصنع.
        /// تُخزَّن مرة واحدة عند تسجيل الدخول لتجنب نداء API إضافي في كل طلب.
        /// </summary>
        protected bool? FactoryIsActive
        {
            get
            {
                var value = HttpContext.Session.GetInt32("FactoryIsActive");
                return value.HasValue ? value.Value == 1 : null;
            }
        }

        protected bool IsLoggedIn =>
            !string.IsNullOrEmpty(Token);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.UserName = FullName;
            ViewBag.UserRole = Role;
            ViewBag.FactoryId = FactoryId;
            ViewBag.FactoryIsActive = FactoryIsActive;
            ViewBag.IsLoggedIn = IsLoggedIn;

            base.OnActionExecuting(context);
        }
    }
}