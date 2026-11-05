using System.Text.Json;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// خدمة إدارة Idempotency للعمليات الحساسة.
/// تضمن عدم تنفيذ نفس العملية أكثر من مرة عند إعادة إرسال الطلب من العميل.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// يتحقق من وجود سجل سابق بنفس المفتاح.
    /// إذا وجد:
    /// - ويعود بنجاح سابق: يعيد النتيجة المخزنة (Payload + StatusCode).
    /// - والبيانات مختلفة (RequestHash): يرمي استثناء (409 Conflict).
    /// - والعملية السابقة فشلت: يسمح بالإعادة إذا كانت السياسة تسمح.
    /// إذا لم يوجد: ينشئ سجلاً جديداً في حالة "قيد التنفيذ" ويعيد null.
    /// </summary>
    /// <typeparam name="TResult">نوع نتيجة العملية.</typeparam>
    /// <param name="key">مفتاح Idempotency المرسل من العميل.</param>
    /// <param name="userId">معرف المستخدم الحالي.</param>
    /// <param name="operationType">نوع العملية (مثال: "CreateOrder", "CreatePayment").</param>
    /// <param name="requestHash">تجزئة بيانات الطلب للتحقق من التطابق.</param>
    /// <returns>
    /// (IsDuplicate, StoredResult, StoredStatusCode).
    /// IsDuplicate=true يعني وجود عملية سابقة مكتملة، ولا ينبغي تنفيذ العملية مجدداً.
    /// </returns>
    Task<(bool IsDuplicate, TResult? Result, int StatusCode)> CheckAndInitializeAsync<TResult>(
        string key,
        int userId,
        string operationType,
        string requestHash);

    /// <summary>
    /// يسجل نجاح العملية ويخزن النتيجة للإرجاع عند التكرار.
    /// </summary>
    Task CompleteAsync<TResult>(
        string key,
        int userId,
        string operationType,
        TResult result,
        int statusCode);

    /// <summary>
    /// يسجل فشل العملية (لإمكانية إعادة المحاولة أو التحقيق).
    /// </summary>
    Task FailAsync(
        string key,
        int userId,
        string operationType,
        string errorMessage);
}