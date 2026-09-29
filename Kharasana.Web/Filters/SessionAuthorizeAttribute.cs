using Kharasana.Application.Common;
using Kharasana.Web.Controllers;
using Kharasana.Web.Localization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.Web.Filters;

/// <summary>
/// حارس وجود الجلسة ودورها في طبقة Web — <b>وليس تحققًا أمنيًا من التوكن</b>.
///
/// <para>ما يفعله: يتأكد أن في الجلسة توكنًا غير منتهٍ (<see cref="TokenIsExpired"/>
/// يقرأ <c>exp</c> من حمولة JWT بلا فحص توقيع)، وأن الدور المخزَّن في الجلسة ضمن
/// الأدوار المطلوبة. يمنع الوصول إلى الصفحات قبل إرسال أي طلب إلى الـ API ويحسّن
/// تجربة المستخدم.</para>
///
/// <para><b>ما لا يفعله:</b> لا يتحقق من توقيع التوكن ولا من المُصدِر ولا من الجمهور،
/// ولا يعتمد عليه في قرار أمني — الحمولة تُقرأ كما هي بلا مفتاح التوقيع
/// (<see cref="JwtSecurityTokenHandler.ReadJwtToken"/>). ودور الجلسة نصٌّ كتبه
/// AccountController عند الدخول، لا ادّعاء موقَّع.</para>
///
/// <para><b>المرجع الأمني هو الـ API</b>: كل نداء يحمل التوكن ويرجع الـ API بفحصه
/// (توقيع/مُصدِر/جمهور/عمر) وبفحص صلاحية المتصل على المورد نفسه — في
/// <c>OrderService.GetOrderOrThrowAsync</c> وعزل المصنع في متحكّمات الـ API.
/// أي تجاوز لهذا الحارس في Web (بجلسة مصنوعة يدويًا) لا يمنح شيئًا: الـ API يردّ 401/403.</para>
///
/// <para>يُستعمل على المتحكّم أو الفعل معًا: قائمة أدوار مفصولة بفواصل تُطابق
/// ثوابت <c>Roles</c> في طبقة Application.</para>
/// </summary>
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var token = session.GetString("Token");

        // التحقق من وجود التوكن وصلاحيته
        if (string.IsNullOrWhiteSpace(token) || TokenIsExpired(token))
        {
            session.Clear();

            // ✅ سبب الخروج: كانت إعادة التوجيه صامتة، فيهبط المستخدم على نموذج الدخول
            //    بلا أي تفسير. صفحة الدخول تعرض TempData[Error] في تنبيه أحمر
            //    (Login.cshtml:44)، فنضع النص قبل إعادة التوجيه لأن TempData يعبر الطلب.
            //
            // ⚠️ لا تخلط بين ثابتين متشابهين في الاسم:
            //    • AppMessages.Common.Unauthorized (هنا) = رسالة انتهاء الجلسة.
            //    • Messages.Unauthorized (Application) = رسالة صلاحية، وتُستعمل في فرع
            //      عدم تطابق الدور أدناه — وهذا موضعها الصحيح.
            if (context.Controller is Controller ctrl)
            {
                ctrl.TempData[BaseController.TempDataError] = AppMessages.Common.Unauthorized;
            }

            RedirectToLogin(context);
            return;
        }

        // التحقق من الدور إذا كان مطلوباً — يدعم قائمة أدوار مفصولة بفواصل (مثل "Admin,FactoryEmployee")
        if (!string.IsNullOrWhiteSpace(_requiredRole))
        {
            var userRole = session.GetString("Role");

            var allowedRoles = _requiredRole
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!allowedRoles.Contains(userRole, StringComparer.Ordinal))
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData[BaseController.TempDataError] = Messages.Unauthorized;
                }

                RedirectToDashboard(context);
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    private static void RedirectToLogin(ActionExecutingContext context)
    {
        context.Result = new RedirectToActionResult(
            actionName: "Login",
            controllerName: "Account",
            routeValues: null);
    }

    private static void RedirectToDashboard(ActionExecutingContext context)
    {
        context.Result = new RedirectToActionResult(
            actionName: "Index",
            controllerName: "Dashboard",
            routeValues: null);
    }

    private static bool TokenIsExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return true;

            var jwt = handler.ReadJwtToken(token);

            // ✅ استخدم Expiration بدلاً من Exp (وهو الطريقة الصحيحة في الإصدارات الجديدة)
            var exp = jwt.Payload.Expiration;

            if (!exp.HasValue)
                return true;

            var expiration = DateTimeOffset.FromUnixTimeSeconds(exp.Value).UtcDateTime;

            return DateTime.UtcNow >= expiration;
        }
        catch
        {
            return true;
        }
    }
}