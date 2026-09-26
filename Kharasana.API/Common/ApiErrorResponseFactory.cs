using Kharasana.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kharasana.API.Common;

/// <summary>
/// يبني ردّ 400 بنفس شكل <see cref="ApiResponse"/> الذي يبنيه ExceptionMiddleware —
/// مصدر واحد لشكل خطأ الـ API.
///
/// <para><b>العلّة التي عالجها:</b> كان في الـ API شكلان لخطأ 400.
/// فشل FluentValidation يمرّ عبر <c>ValidationFilter</c> فيرمي
/// <c>ValidationException</c> ويلتقطه ExceptionMiddleware فيُردّ
/// <c>{ success: false, message: "نصّ عربي" }</c>. أما فشل ModelState فيتولّاه
/// فلتر <c>[ApiController]</c> المدمج (يعمل بترتيب -2000، أي <i>قبل</i> فلتر
/// FluentValidation) فيردّ <c>ValidationProblemDetails</c> بصيغة RFC 7807:
/// <c>{ title, status, errors: { Field: [...] } }</c> — بلا حقل <c>message</c>.</para>
///
/// <para><b>الأثر على المستخدم:</b> واجهة الويب تقرأ <c>message</c> من جسم الخطأ
/// (انظر <c>ApiClient.HandleResponseAsync</c>)؛ وعند غيابه تسقط إلى رسالة عامة
/// «تعذر تنفيذ العملية». فرسالة «الاسم الكامل مطلوب.» المُعرَّفة في
/// <c>RegisterUserDto</c> كانت تُفقد وتظهر للمستخدم رسالة لا تصف المشكلة.
/// ويشمل ذلك أيضاً أخطاء تحويل JSON التي لا تحمل نصاً عربياً أصلاً
/// («The JSON value could not be converted to…») — ولذلك تُستبدل هنا
/// برسالة عربية عامة بدل تسريب نصّ تقني إنجليزي.</para>
/// </summary>
public static class ApiErrorResponseFactory
{
    /// <summary>
    /// يحوّل ModelState فاشلاً إلى 400 بشكل ApiResponse.
    /// يُفضّل أول رسالة عربية مكتوبة (سمات DataAnnotations) على رسائل التحويل التقنية،
    /// ويسقط إلى <see cref="Messages.InvalidRequest"/> إن لم توجد رسالة صالحة للعرض.
    /// </summary>
    public static IActionResult FromModelState(ModelStateDictionary modelState)
    {
        var message = FirstDisplayableMessage(modelState) ?? Messages.InvalidRequest;

        return new BadRequestObjectResult(ApiResponse.Fail(message));
    }

    /// <summary>
    /// أول رسالة خطأ عربية في ModelState — أو null إن كانت كل الأخطاء تقنية.
    /// </summary>
    private static string? FirstDisplayableMessage(ModelStateDictionary modelState)
    {
        return modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(IsDisplayable);
    }

    /// <summary>
    /// رسالة صالحة للعرض على المستخدم: غير فارغة، ومكتوبة بالعربية.
    /// رسائل فشل التحويل (JsonException / Exception) تكون إنجليزية تقنية،
    /// ورسالة <c>[Required]</c> الافتراضية كذلك («The FullName field is required.»)
    /// إن كُتبت سمة بلا ErrorMessage عربي.
    /// </summary>
    private static bool IsDisplayable(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Any(c => c >= 0x0600 && c <= 0x06FF);
    }
}
