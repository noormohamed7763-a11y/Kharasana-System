using System.Security.Claims;
using Kharasana.Domain.Enums;
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
            HttpContext.Session.GetString("Token"); // Token لا يزال في الجلسة

        protected string? FullName =>
            User.FindFirst(ClaimTypes.Name)?.Value;

        protected string? Role =>
            User.FindFirst(ClaimTypes.Role)?.Value;

        /// <summary>
        /// الدور الحالي قيمةً من UserRole بدل مقارنة النصوص مباشرة.
        /// null إذا كانت القيمة غير معروفة.
        /// </summary>
        protected UserRole? RoleValue =>
            Enum.TryParse(Role, ignoreCase: false, out UserRole role) ? role : null;

        protected int? FactoryId =>
            int.TryParse(User.FindFirst("FactoryId")?.Value, out var id) ? id : null;

        protected int? CurrentUserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        /// <summary>
        /// هل الكيان المطلوب معزول عن المستخدم الحالي؟
        /// صحيح إذا كان المستخدم موظف أو مدير مصنعٍ والكيان يتبع مصنعاً مختلفاً —
        /// فعادةً يُقابَل برفض الوصول (Forbidden). للمدراء تعيد false دائماً.
        /// </summary>
        protected bool IsFactoryIsolated(int? entityFactoryId) =>
            IsFactoryUser && entityFactoryId != FactoryId;

        /// <summary>
        /// هل المستخدم الحالي مرتبط بمصنع (موظف أو مدير مصنع)؟
        /// </summary>
        protected bool IsFactoryUser =>
            RoleValue == UserRole.FactoryEmployee || RoleValue == UserRole.FactoryAdmin;

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
            // CSP nonce — يُعرض في الـ Views للسماح بـ scripts داخلية بدون unsafe-inline
            if (HttpContext.Items.TryGetValue("CspNonce", out var nonceObj) &&
                nonceObj is string nonce)
            {
                ViewBag.CspNonce = nonce;
            }

            ViewBag.UserName = FullName;
            ViewBag.UserRole = Role;
            ViewBag.FactoryId = FactoryId;
            ViewBag.FactoryIsActive = FactoryIsActive;
            ViewBag.FactoryLogo = HttpContext.Session.GetString("FactoryLogo");

            // (أُزيل ViewBag.IsLoggedIn: كان يُكتب ولا يقرأه أي view. الخاصية IsLoggedIn
            //  نفسها حيّة وتُستعمل في AccountController وHomeController — المحذوف نسخة
            //  الـViewBag وحدها.)

            base.OnActionExecuting(context);
        }
    }
}