using Kharasana.Web.Controllers;
using Kharasana.Web.Localization;
using Kharasana.Web.Models;
using Kharasana.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Kharasana.Web.Filters;

/// <summary>
/// فلتر عالمي لالتقاط ApiServiceException الهاربة من أي controller action.
/// يضمن أن المستخدم لا يرى صفحة خطأ 500 فارغة على أخطاء API المتوقعة.
/// - AJAX/fetch → JSON { success=false, message }
/// - 401 → تفريغ الجلسة ثم إعادة توجيه لصفحة تسجيل الدخول
/// - POST → إعادة توجيه إلى الصفحة السابقة مع TempData error
/// - ما عدا ذلك (GET وغيره) → صفحة الخطأ في مكانها بلا إعادة توجيه
///
/// <para><b>ضمان عدم الدوران:</b> إعادة التوجيه محصورة في POST، والهدف يُطلَب
/// بعدها بـ GET — وردّ الفلتر على الطلب الفاشل غير الـ POST هو عرض صفحة الخطأ
/// نفسها لا إعادة توجيه. فتنتهي السلسلة عند خطوة واحدة مهما تكرّر الفشل، ولا
/// يمكن أن تنشأ حلقة توجيه مهما كانت الصفحات فاشلة.</para>
/// </summary>
public sealed class UnhandledExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<UnhandledExceptionFilter> _logger;

    public UnhandledExceptionFilter(ILogger<UnhandledExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiServiceException apiEx)
            return Task.CompletedTask; // أخطاء غير ApiService تنتقل لـ UseExceptionHandler (500)

        context.ExceptionHandled = true;

        _logger.LogWarning(
            "التقط الفلتر ApiServiceException غير معالَج: الحالة={StatusCode} الكود={ErrorCode} الرسالة={Message} التتبّع={TraceId}",
            (int)apiEx.StatusCode,
            apiEx.Error.ErrorCode,
            apiEx.Error.FormatMessage(apiEx.MessageArgs ?? Array.Empty<object>()),
            apiEx.TraceId);

        // ── AJAX / fetch ──
        if (IsAjaxRequest(context.HttpContext))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                errorCode = apiEx.Error.ErrorCode,
                message = apiEx.Error.FormatMessage(),
                solution = apiEx.Error.UserSolution,
                traceId = apiEx.TraceId
            })
            { StatusCode = (int)apiEx.StatusCode };
            return Task.CompletedTask;
        }

        var httpContext = context.HttpContext;

        // ── 401 Unauthorized → تسجيل الدخول ──
        if (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // ✅ حذف التوكن فقط بدل تفريغ كامل الجلسة:
            //    صفحة الدخول تُحوِّل المسجَّل فورًا إلى اللوحة، فإن بقي توكن منتهٍ في الجلسة
            //    عاد المستخدم إليها وفشل نداؤها من جديد: Login ↔ Dashboard بلا نهاية.
            //    حذف التوكن يكسر هذه الحلقة دون المساس ببيانات الجلسة الأخرى (إن وجدت).
            httpContext.Session.Remove("Token");

            context.Result = new RedirectToActionResult("Login", "Account", null);
            return Task.CompletedTask;
        }

        // الرسالة تُسلَّم عبر TempData لا HttpContext.Items: فرع POST ينتهي بإعادة
        // توجيه، و Items لا يعبر إلى الطلب التالي فتضيع الرسالة قبل أن يقرأها
        // Components/_Alerts. وكذلك فرع صفحة الخطأ: لا يقرأ TempData هناك (تخطيطه
        // null)، فتبقى الرسالة للصفحة التالية التي يفتحها المستخدم.
        // نفس الطريقة المتبعة في Program.cs (RateLimiter.OnRejected).
        var tempData = httpContext.RequestServices
            .GetRequiredService<ITempDataDictionaryFactory>()
            .GetTempData(httpContext);

        tempData[BaseController.TempDataError] = apiEx.Error.FormatMessage(apiEx.MessageArgs ?? Array.Empty<object>());
        tempData["ErrorSolution"] = apiEx.Error.UserSolution;
        tempData["TraceId"] = apiEx.TraceId;
        tempData.Save();

        // ── POST: نُعيد المستخدم إلى الصفحة السابقة مع رسالة الخطأ ──
        //
        // ⚠️ إعادة التوجيه محصورة في POST عمدًا: الهدف يُطلَب بعدها بـ GET، وردّ هذا
        //    الفلتر على أي طلب فاشل آخر (GET خصوصًا) هو عرض صفحة الخطأ في مكانها بلا
        //    إعادة توجيه — فلا تدور السلسلة أبدًا.
        //
        //    ما كان قبل ذلك: كل طلب فاشل — GET أيضًا — يُحوَّل إلى الصفحة السابقة،
        //    وإلا إلى "/Dashboard". وحين تفشل اللوحة نفسها (نداء الـ API الذي تُبنى
        //    عليه) يصير الهدف هو الصفحة الفاشلة ذاتها: تُطلَب من جديد، وتفشل، وتُحوَّل
        //    إلى نفسها… حتى يعرض المتصفح ERR_TOO_MANY_REDIRECTS. وكان فحص Referer
        //    لا ينقذ الموقف لأنه يقارن رابطًا مطلقًا (ما يرسله المتصفح فعلًا) بمسار
        //    محلي، فيسقط الشرط ويهبط دائمًا على "/Dashboard" — أي على الصفحة الفاشلة.
        if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            context.Result = new RedirectResult(ResolvePostRedirectTarget(httpContext));
            return Task.CompletedTask;
        }

        // ── ما عدا ذلك: صفحة الخطأ في مكانها (بلا إعادة توجيه ⇒ بلا حلقة) ──
        context.Result = BuildErrorResult(apiEx, context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// نتيجة صفحة الخطأ نفسها التي يعرضها <c>HomeController.Error</c>، مبنيّة هنا
    /// لإعادة استعمالها من الفلتر مباشرة بلا إعادة توجيه.
    /// الرمز يُرفع إلى 500 إن جاء من الـ API رمز نجاح (يحدث حين يفشل تحليل ردّ ناجح
    /// في ApiClient) فلا تُعلَن استجابة فاشلة بنجاح.
    /// </summary>
    private static ViewResult BuildErrorResult(ApiServiceException apiEx, ExceptionContext context)
    {
        var statusCode = (int)apiEx.StatusCode;
        if (statusCode < StatusCodes.Status400BadRequest)
            statusCode = StatusCodes.Status500InternalServerError;

        return new ViewResult
        {
            ViewName = "Error",
            ViewData = new ViewDataDictionary<ErrorViewModel>(
                new EmptyModelMetadataProvider(),
                context.ModelState)
            {
                Model = new ErrorViewModel
                {
                    RequestId = apiEx.TraceId,
                    StatusCode = statusCode
                }
            },
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// هدف إعادة التوجيه بعد فشل طلب POST: الصفحة السابقة إن كانت محلية، وإلا
    /// لوحة التحكم (أو صفحة الدخول لزائر غير مسجَّل).
    /// </summary>
    private static string ResolvePostRedirectTarget(HttpContext httpContext)
    {
        if (TryGetLocalReferrerPath(httpContext, out var referrerPath))
            return referrerPath;

        var isLoggedIn = !string.IsNullOrWhiteSpace(httpContext.Session.GetString("Token"));

        return isLoggedIn ? "/Dashboard" : "/Account/Login";
    }

    /// <summary>
    /// يستخرج مسارًا محليًا (بمعاملات الاستعلام) من ترويسة Referer إن كانت للطلب نفسه.
    /// </summary>
    private static bool TryGetLocalReferrerPath(HttpContext httpContext, out string path)
    {
        path = string.Empty;

        var referrer = httpContext.Request.Headers.Referer.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(referrer))
            return false;

        // المتصفح يرسل رابطًا مطلقًا: يُقبل فقط إذا كان مضيفه مضيف الطلب نفسه،
        // وإلا صار الفلتر بابًا لإعادة توجيه مفتوحة إلى موقع خارجي.
        if (Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
        {
            if (!string.Equals(uri.Authority, httpContext.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
                return false;

            path = uri.PathAndQuery;
            return true;
        }

        // بعض العملاء يرسلون مسارًا نسبيًا. ✅ مسار محلي فقط: "‎//evil.com‎" و"‎/\evil.com‎"
        // يفسّرهما المتصفح كرابط خارجي مطلق (protocol-relative)، فـ StartsWith("/")
        // وحده لا يمنع إعادة توجيه مفتوحة.
        if (!referrer.StartsWith('/')
            || referrer.StartsWith("//", StringComparison.Ordinal)
            || referrer.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        path = referrer;
        return true;
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        if (string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var accept = context.Request.Headers.Accept.ToString();
        if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }
}
