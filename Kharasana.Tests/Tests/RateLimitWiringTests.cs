using System.Reflection;
using Kharasana.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حارس التوصيل — يمنع تكرار علّة «وعد بـ429 في التوثيق بلا تحديد معدّل فعلي».
///
/// <para>العلّة الأصلية: توثيق Swagger لنقطة <c>POST /api/auth/register</c> كان يَعِد
/// بـ<c>429</c> («تجاوز حد المحاولات المسموح في الدقيقة»)، ولم يكن على الدالة أي
/// <c>[EnableRateLimiting]</c>. الوعد لم يكن يتحقق إلا عبر <c>GlobalLimiter</c> العام
/// (100 طلب/دقيقة لكل IP) — وهو أوسع من أن يمنع تعداد الحسابات: التسجيل يردّ 409
/// (بريد/هاتف مسجَّل) مقابل 201، فيكفي المهاجمَ أن يجرّب العناوين ليعرف أيها مسجَّل.</para>
///
/// <para>الاختبار يفحص بالانعكاس كل نقطة نهاية في <c>Kharasana.API.Controllers</c>
/// مجهولة (وحدتها تحمل <c>[AllowAnonymous]</c>) وتكتب (POST) ولا تعيد فرض المصادقة،
/// ويوجب أن تحمل سياسة تحديد معدّل باسم غير فارغ. إضافة نقطة مجهولة جديدة ونسيان
/// السياسة تُسقط هذا الاختبار فوراً.</para>
///
/// <para>النمط مأخوذ من <see cref="ValidatorWiringTests"/>: نفس فكرة «سمة توصيل
/// ناقصة = ميزة تبدو موجودة ولا تعمل».</para>
/// </summary>
public class RateLimitWiringTests
{
    private static readonly Assembly ApiAssembly = typeof(ValidationFilter<>).Assembly;

    /// <summary>
    /// (الوحدة، الدالة، السياسة) لكل نقطة نهاية مجهولة تكتب ولا تعيد فرض المصادقة.
    /// تُقبل السمة على الدالة أو على الوحدة (كلاهما صالح في MVC).
    /// </summary>
    private static List<(Type Controller, MethodInfo Action, string? PolicyName)> AnonymousWriteEndpoints()
    {
        var endpoints = new List<(Type, MethodInfo, string?)>();

        var controllers = ApiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == "Kharasana.API.Controllers")
            .ToList();

        foreach (var controller in controllers)
        {
            // النقطة مجهولة فقط إن كانت وحدتها [AllowAnonymous]
            if (controller.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) == null)
                continue;

            var controllerPolicy = controller
                .GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true)?.PolicyName;

            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                         .Where(m => !m.IsSpecialName && m.DeclaringType == controller))
            {
                var isPost = action.GetCustomAttributes<HttpMethodAttribute>(inherit: true)
                    .Any(a => a.HttpMethods.Contains("POST", StringComparer.OrdinalIgnoreCase));
                if (!isPost)
                    continue;

                // استثناء صريح: دالة تعيد فرض المصادقة ليست مجهولة فعلاً
                if (action.GetCustomAttribute<AuthorizeAttribute>(inherit: true) != null)
                    continue;

                var policy = action.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true)?.PolicyName
                             ?? controllerPolicy;

                endpoints.Add((controller, action, policy));
            }
        }

        return endpoints;
    }

    [Fact]
    public void Reflection_FindsTheExpectedAnonymousEndpoints()
    {
        // حماية من «نجاح كاذب»: لو تغيّر اسم مساحة الأسماء أو أسلوب الاستخراج
        // لعادت القائمة فارغة ومرّ الاختبار التالي بلا فحص أي شيء.
        var endpoints = AnonymousWriteEndpoints();

        endpoints.Should().HaveCountGreaterThanOrEqualTo(2,
            "AuthController فيه نقطتان مجهولتان تكتبان: Register و Login");

        endpoints.Select(e => e.Action.Name).Should().Contain(["Register", "Login"]);
    }

    [Fact]
    public void EveryAnonymousWriteEndpoint_HasARateLimitPolicy()
    {
        var missing = AnonymousWriteEndpoints()
            .Where(e => e.PolicyName == null)
            .Select(e => $"{e.Controller.Name}.{e.Action.Name} نقطة مجهولة بلا " +
                         "[EnableRateLimiting] — لا يحده إلا الحاجز العام الواسع")
            .ToList();

        missing.Should().BeEmpty(
            "نقطة مجهولة بلا تحديد معدّل خاص = تعداد الحسابات ممكن بالجملة، " +
            "وتوثيقها قد يَعِد بـ429 لا يتحقق");
    }

    [Fact]
    public void EveryRateLimitPolicyOnAnonymousEndpoints_HasAName()
    {
        // سمة بلا اسم سياسة لا تطابق أي سياسة مسجَّلة في Program.cs — الحاجز لا يُطبَّق
        // إطلاقاً، وهي أسوأ من غياب السمة لأنها تُوهم القارئ بأن الحماية موجودة.
        var unnamed = AnonymousWriteEndpoints()
            .Where(e => e.PolicyName != null && string.IsNullOrWhiteSpace(e.PolicyName))
            .Select(e => $"{e.Controller.Name}.{e.Action.Name} يحمل [EnableRateLimiting] بلا اسم سياسة")
            .ToList();

        unnamed.Should().BeEmpty("سياسة بلا اسم لا تُطابق شيئاً — حماية وهمية");
    }
}
