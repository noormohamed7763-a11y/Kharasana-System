using System.Reflection;
using FluentValidation;
using FluentAssertions;
using Kharasana.API.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حارس التوصيل — يمنع تكرار علّة «مُدقّقات موجودة لا تُشغَّل أبداً».
///
/// <para>العلّة الأصلية: 17 مُدقّقاً في <c>Kharasana.Application.Validators</c>،
/// مُسجَّلة كلها في DI عبر <c>AddValidatorsFromAssemblyContaining</c>، لكن
/// FluentValidation لا يعمل تلقائياً هنا (لا <c>AddFluentValidationAutoValidation</c>) —
/// فيعمل فقط حيث يوضع <c>[ServiceFilter(typeof(ValidationFilter&lt;T&gt;))]</c> صراحةً.
/// وكان ذلك على OrdersController وحده: 7 مُدقّقات تعمل و10 لا تعمل إطلاقاً،
/// ومنها <c>CreateUserDtoValidator</c> — فكان <c>POST /api/Users</c> يقبل كلمة مرور
/// من محرف واحد.</para>
///
/// <para>هذا الاختبار يفحص بالانعكاس كل نقطة نهاية في <c>Kharasana.API.Controllers</c>
/// تستقبل جسماً (<c>[FromBody]</c>): إن كان لنوعه مُدقّق مسجَّل في طبقة Application
/// وجب أن تحمل الدالة فلتر التحقق الخاص به. إضافة DTO له مُدقّق ونسيان الفلتر
/// تُسقط هذا الاختبار فوراً.</para>
/// </summary>
public class ValidatorWiringTests
{
    private static readonly Assembly ApiAssembly = typeof(ValidationFilter<>).Assembly;
    private static readonly Assembly ApplicationAssembly =
        typeof(Kharasana.Application.Validators.Common.ValidationRules).Assembly;

    /// <summary>(نوع الـ DTO، الدالة، الفلتر المتوقّع) لكل نقطة نهاية تستقبل جسماً ولها مُدقّق.</summary>
    private static List<(Type Dto, MethodInfo Action, Type ExpectedFilter)> ValidatedBodyActions()
    {
        var pairs = new List<(Type, MethodInfo, Type)>();

        var controllers = ApiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == "Kharasana.API.Controllers")
            .ToList();

        foreach (var controller in controllers)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                         .Where(m => !m.IsSpecialName && m.DeclaringType == controller))
            {
                // نقطة نهاية فعلية = عليها أي سمة من سمات أفعال HTTP
                var isEndpoint = action.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any();
                if (!isEndpoint)
                    continue;

                foreach (var parameter in action.GetParameters())
                {
                    var isFromBody = parameter.GetCustomAttribute<FromBodyAttribute>(inherit: true) != null;
                    if (!isFromBody)
                        continue;

                    var dto = parameter.ParameterType;
                    if (ValidatorExistsFor(dto))
                        pairs.Add((dto, action, typeof(ValidationFilter<>).MakeGenericType(dto)));
                }
            }
        }

        return pairs;
    }

    /// <summary>هل يوجد مُدقّق مسجَّل لهذا النوع؟ (نفس نطاق AddValidatorsFromAssemblyContaining)</summary>
    private static bool ValidatorExistsFor(Type dto)
        => ApplicationAssembly.GetTypes().Any(t =>
            t is { IsClass: true, IsAbstract: false }
            && typeof(IValidator<>).MakeGenericType(dto).IsAssignableFrom(t));

    private static ServiceFilterAttribute? FilterFor(MethodInfo action, Type filterType)
        => action.GetCustomAttributes<ServiceFilterAttribute>(inherit: true)
            .FirstOrDefault(a => a.ServiceType == filterType);

    [Fact]
    public void Reflection_FindsTheExpectedValidatedEndpoints()
    {
        // حماية من «نجاح كاذب»: لو تغيّر اسم مساحة الأسماء أو أسلوب الاستخراج
        // لعادت القائمة فارغة ومرّ الاختبار التالي بلا فحص أي شيء.
        var pairs = ValidatedBodyActions();

        pairs.Should().HaveCountGreaterThanOrEqualTo(17,
            "النظام يحتوي 17 مُدقّقاً لـ DTOs تصل في جسم الطلب (10 كانت معطّلة قبل الإصلاح)");

        pairs.Select(p => p.Action.Name).Should()
            .Contain(["Register", "Login", "Create", "Update", "UpdateMyProfile", "UpdateDriverStatus"]);
    }

    [Fact]
    public void EveryBodyDtoWithValidator_HasItsValidationFilter()
    {
        var missing = ValidatedBodyActions()
            .Where(p => FilterFor(p.Action, p.ExpectedFilter) == null)
            .Select(p => $"{p.Action.DeclaringType!.Name}.{p.Action.Name} " +
                         $"يستقبل {p.Dto.Name} وله مُدقّق، لكن بلا " +
                         $"[ServiceFilter(typeof(ValidationFilter<{p.Dto.Name}>))]")
            .ToList();

        missing.Should().BeEmpty(
            "مُدقّق غير مُشغَّل = تحقق ميت: يبدو موجوداً في الكود ولا يُنفَّذ أبداً على أي طلب");
    }

    [Fact]
    public void AllValidationFilters_TargetADtoThatActuallyAppearsInTheBody()
    {
        // الاتجاه المعاكس: فلتر على دالة لا تستقبل ذلك الـ DTO لا يفعل شيئاً
        // (ValidationFilter يبحث عن الوسيط بنوع T، ولا يجد شيئاً فيمرّ الطلب بلا تحقق) —
        // أي فلتر مكتوب بنوع خاطئ هو تحقق ميت أيضاً.
        var wiredPairs = ValidatedBodyActions()
            .Select(p => (p.Action, Filter: p.ExpectedFilter))
            .ToHashSet();

        var orphans = new List<string>();

        foreach (var controller in ApiAssembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false }
                                 && t.Namespace == "Kharasana.API.Controllers"))
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var filter in action.GetCustomAttributes<ServiceFilterAttribute>(inherit: true))
                {
                    if (!filter.ServiceType.IsGenericType
                        || filter.ServiceType.GetGenericTypeDefinition() != typeof(ValidationFilter<>))
                        continue;

                    if (!wiredPairs.Contains((action, filter.ServiceType)))
                    {
                        orphans.Add($"{controller.Name}.{action.Name} يحمل فلتر " +
                                    $"{filter.ServiceType.GetGenericArguments()[0].Name} " +
                                    "ولا يستقبل هذا النوع في جسم الطلب");
                    }
                }
            }
        }

        orphans.Should().BeEmpty("فلتر بنوع غير مطابق لا يفحص شيئاً — تحقق ميت بصيغة أخرى");
    }

    [Fact]
    public void AllValidatorsInApplicationAssembly_AreReachableFromAnEndpoint()
    {
        // لا مُدقّق مكتوب بلا نقطة نهاية تشغّله. الاستثناء الوحيد المقصود محذوف
        // من الحساب: لا شيء حالياً — كل الـ 17 مُدقّقاً مرتبط بنقطة نهاية.
        var validatorDtos = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => i.GetGenericArguments()[0]))
            .Distinct()
            .ToList();

        var wiredDtos = ValidatedBodyActions().Select(p => p.Dto).Distinct().ToList();

        var unreachable = validatorDtos.Except(wiredDtos)
            .Select(dto => $"{dto.Name} له مُدقّق ولا توجد نقطة نهاية تستقبله وتشغّله")
            .ToList();

        unreachable.Should().BeEmpty();
    }
}
