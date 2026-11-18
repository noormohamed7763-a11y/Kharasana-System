using System.Security.Claims;
using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;

namespace Kharasana.API.Extensions;

/// <summary>
/// ملحقات استخراج هوية الطالب من ClaimsPrincipal.
/// نمط الخطأ هنا استثنائي (UnauthorizedException) لا قيمة إرجاع IActionResult:
/// هذا هو «النمط الموجود مسبقاً» في المشروع — الاستثناءات تُرمى من الطبقات الداخلية
/// ويحوّلها ExceptionMiddleware إلى رموز HTTP، وربط <see cref="UnauthorizedException"/>
/// بـ 401 قائم في فرعه المخصّص.
/// </summary>
public static class CallerContextExtensions
{
    /// <summary>
    /// يستخرج <see cref="CallerContext"/> من جلسة الطلب، أو يرمي
    /// <see cref="UnauthorizedException"/> عند نقص أي من ادعاءات الهوية
    /// (جلسة منتهية/نصف مكتملة/هوية غير صالحة).
    /// </summary>
    public static CallerContext GetCallerContext(this ClaimsPrincipal user)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var userId = user.GetUserId();
        if (userId is null)
            throw new UnauthorizedException("جلسة غير صالحة — معرّف المستخدم مفقود.");

        if (!user.TryGetRole(out var role))
            throw new UnauthorizedException("جلسة غير صالحة — دور المستخدم غير معروف.");

        var factoryId = user.GetFactoryId();

        return new CallerContext(userId.Value, role, factoryId);
    }
}
