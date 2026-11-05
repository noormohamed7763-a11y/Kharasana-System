using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kharasana.Domain.Entities;

/// <summary>
/// سجل العمليات المكتملة لضمان Idempotency.
/// يمنع تنفيذ نفس العملية الحساسة (إنشاء طلب، تسجيل دفعة) أكثر من مرة.
/// </summary>
public class IdempotencyRecord
{
    public int Id { get; set; }

    /// <summary>
    /// مفتاح Idempotency الفريد المرسل من العميل (Flutter/MVC).
    /// يجب أن يكون فريداً لكل مستخدم + نوع عملية.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// معرف المستخدم الذي أرسل العملية.
    /// يضمن عدم إمكانية مستخدم استخدام مفتاح مستخدم آخر.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// نوع العملية: "CreateOrder", "CreatePayment", "CreatePhoneOrder" وغيرها.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// تجزئة (Hash) بيانات الطلب للتحقق من تطابق البيانات عند إعادة الإرسال.
    /// إذا كان المفتاح مستخدماً مع بيانات مختلفة، يُرفض الطلب.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string RequestHash { get; set; } = string.Empty;

    /// <summary>
    /// المعرف الناتج عن العملية الناجحة (OrderId, PaymentId, إلخ).
    /// يستخدم لإرجاع نفس النتيجة عند إعادة الإرسال.
    /// </summary>
    public string? ResultEntityId { get; set; }

    /// <summary>
    /// نتيجة العملية كاملة (مسلسل JSON) للإرجاع عند التكرار.
    /// </summary>
    [MaxLength(4000)]
    public string? ResultPayload { get; set; }

    /// <summary>
    /// رمز حالة HTTP للعملية الأصلية (201, 200, 400, إلخ).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// وقت إنشاء السجل (وقت نجاح العملية لأول مرة).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// وقت انتهاء صلاحية السجل (اختياري - للتنظيف الدوري).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}