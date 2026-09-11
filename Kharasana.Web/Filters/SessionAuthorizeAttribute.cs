using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.Web.Filters;

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
            RedirectToLogin(context);
            return;
        }

        // التحقق من الدور إذا كان مطلوباً
        if (!string.IsNullOrWhiteSpace(_requiredRole))
        {
            var userRole = session.GetString("Role");

            if (!string.Equals(userRole, _requiredRole, StringComparison.Ordinal))
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["ErrorMessage"] =
                        "عذراً، ليس لديك صلاحية للوصول إلى هذه الصفحة.";
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

            if (exp is null)
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