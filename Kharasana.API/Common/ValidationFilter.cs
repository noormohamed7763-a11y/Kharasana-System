using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kharasana.API.Common;

/// <summary>
/// فلتر تحقّق يُشغّل FluentValidation على DTO الدخول قبل وصوله للخدمة —
/// مصدر التحقق الوحيد. عند الفشل يرمي <c>FluentValidation.ValidationException</c>
/// يلتقطها ExceptionMiddleware وتُردّ بنفس شكل ApiResponse
/// (400 + Success=false + رسالة الخطأ الأول فقط).
/// </summary>
/// <typeparam name="T">نوع نموذج الدخول المراد فحصه (يجب أن يكون له فاحص مسجّل في DI).</typeparam>
public class ValidationFilter<T> : IAsyncActionFilter
    where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // البحث عن معامل الإجراء المطابق لنوع الفاحص (تجاهل المعاملات الأخرى كـ id)
        var dto = context.ActionArguments.Values.OfType<T>().FirstOrDefault();
        if (dto is not null)
        {
            await _validator.ValidateAndThrowAsync(dto);
        }

        await next();
    }
}