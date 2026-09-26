using Kharasana.API.Common;
using Kharasana.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حراسة شكل استجابة 400 الموحّد.
///
/// <para>كان في الـ API شكلان مختلفان لخطأ 400: فشل FluentValidation يمرّ عبر
/// <c>ValidationFilter</c> ← <c>ValidationException</c> ← <c>ExceptionMiddleware</c>
/// فيُردّ <c>ApiResponse</c> بحقل <c>message</c> عربي؛ وفشل ModelState يتولّاه فلتر
/// <c>[ApiController]</c> المدمج (ترتيب -2000، أي قبل فلتر FluentValidation) فيردّ
/// <c>ValidationProblemDetails</c> بصيغة RFC 7807 بلا حقل <c>message</c>. وواجهة الويب
/// تقرأ <c>message</c> من جسم الخطأ، فتسقط عند غيابه إلى رسالة عامة
/// («تعذر تنفيذ العملية») وتفقد نصّ الخطأ الحقيقي.</para>
///
/// <para>هذه الاختبارات تثبّت أن الردّين صارا شكلاً واحداً، وتمنع تسرّب رسائل
/// المحلّل الإنجليزية إلى المستخدم.</para>
/// </summary>
public class ApiErrorResponseFactoryTests
{
    [Fact]
    public void FromModelState_ArabicAnnotationMessage_IsPreserved()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("FullName", "الاسم الكامل مطلوب.");

        var (statusCode, response) = Act(modelState);

        statusCode.Should().Be(400);
        response.Success.Should().BeFalse("شكل الخطأ في ExceptionMiddleware يحمل Success=false دائماً");
        response.Message.Should().Be("الاسم الكامل مطلوب.",
            "رسالة السمة العربية هي ما يجب أن يصل المستخدم — لا رسالة عامة");
    }

    [Fact]
    public void FromModelState_TechnicalOnlyErrors_FallBackToArabicMessage()
    {
        // هذا ما ينتجه فشل تحويل JSON فعلاً — نصّ إنجليزي تقني لا يجوز عرضه للمستخدم
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("$", "The JSON value could not be converted to System.Int32.");

        var (statusCode, response) = Act(modelState);

        statusCode.Should().Be(400);
        response.Message.Should().Be(Messages.InvalidRequest);
        response.Message.Should().NotContain("JSON", "لا يُسرَّب نصّ تقني إنجليزي إلى الواجهة");
    }

    [Fact]
    public void FromModelState_EmptyModelState_StillReturnsArabicBadRequest()
    {
        var (statusCode, response) = Act(new ModelStateDictionary());

        statusCode.Should().Be(400);
        response.Message.Should().Be(Messages.InvalidRequest);
    }

    [Fact]
    public void FromModelState_PrefersArabicMessageEvenWhenATechnicalErrorComesFirst()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Quantity", "The value 'abc' is not valid.");
        modelState.AddModelError("Phone", "رقم الهاتف غير صالح.");

        var (_, response) = Act(modelState);

        response.Message.Should().Be("رقم الهاتف غير صالح.",
            "المرور على الأخطاء يختار أول رسالة عربية لا أول خطأ مطلقاً");
    }

    [Fact]
    public void FromModelState_ReturnsBadRequestObjectResult()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "البريد الإلكتروني غير صالح.");

        var result = ApiErrorResponseFactory.FromModelState(modelState);

        // نفس النوع الذي ينتجه ExceptionMiddleware (ApiResponse.Fail + 400) — لا ProblemDetails
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
        badRequest.Value.Should().BeOfType<ApiResponse<object>>();
    }

    /// <summary>ينفّذ المصنع ويعيد حالة HTTP والجسم بنوعه الموحّد.</summary>
    private static (int StatusCode, ApiResponse<object> Response) Act(ModelStateDictionary modelState)
    {
        var result = ApiErrorResponseFactory.FromModelState(modelState)
            .Should().BeOfType<BadRequestObjectResult>().Subject;

        return (result.StatusCode ?? 0, result.Value.Should().BeOfType<ApiResponse<object>>().Subject);
    }
}
