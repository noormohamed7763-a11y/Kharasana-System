using System.Net;

namespace Kharasana.Web.Services.Api;

/// <summary>
/// قاموس مركزي لتعريفات أخطاء الـ API الموحّدة.
/// </summary>
public static class ApiErrorCatalog
{
    public static readonly ApiError Unknown = new(
        "UNKNOWN_ERROR",
        "حدث خطأ غير متوقع.",
        "يرجى المحاولة مرة أخرى، إذا استمرت المشكلة تواصل مع الدعم الفني.");

    public static readonly ApiError Unauthorized = new(
        "UNAUTHORIZED",
        "انتهت جلستك أو أنك غير مسجّل الدخول.",
        "يرجى تسجيل الدخول مجدداً لاستئناف العمل.");

    public static readonly ApiError Forbidden = new(
        "FORBIDDEN",
        "عذراً، ليس لديك صلاحية للقيام بهذا الإجراء.",
        "تأكد من صلاحيات حسابك أو تواصل مع مدير النظام إذا كنت تعتقد أن هذا خطأ.");

    public static readonly ApiError NotFound = new(
        "NOT_FOUND",
        "العنصر المطلوب (رقم {0}) غير موجود.",
        "تأكد من المعرّف المستخدم أو حاول البحث عنه مجدداً.");

    public static readonly ApiError ServerError = new(
        "SERVER_ERROR",
        "حدث خطأ داخلي في النظام.",
        "يرجى المحاولة مجدداً لاحقاً، إذا استمرت المشكلة تواصل مع الدعم الفني.");

    public static readonly ApiError NetworkError = new(
        "NETWORK_ERROR",
        "تعذر الاتصال بالخادم.",
        "تحقق من اتصالك بالإنترنت وحاول مرة أخرى.");

    public static readonly ApiError InvalidLogin = new(
        "INVALID_LOGIN",
        "البريد الإلكتروني أو كلمة المرور غير صحيحة.",
        "يرجى التحقق من البيانات والمحاولة مجدداً.");

    public static readonly ApiError OrderStatusConflict = new(
        "ORDER_STATUS_CONFLICT",
        "لا يمكن تعديل الطلب رقم #{0} لأنه حالياً في حالة '{1}'.",
        "يرجى مراجعة حالة الطلب قبل محاولة التعديل.");

    // أخطاء العملاء
    public static readonly ApiError ClientNotFound = new(
        "CLIENT_NOT_FOUND",
        "العميل المطلوب (رقم {0}) غير موجود.",
        "تأكد من صحة معرف العميل المختار.");

    public static readonly ApiError ClientCreateFailed = new(
        "CLIENT_CREATE_FAILED",
        "فشل إنشاء حساب العميل: {0}",
        "تأكد من عدم تكرار البريد الإلكتروني أو رقم الهاتف.");

    public static readonly ApiError ClientUpdateFailed = new(
        "CLIENT_UPDATE_FAILED",
        "فشل تحديث بيانات العميل (رقم {0}): {1}",
        "تأكد من صحة البيانات المدخلة والمحاولة مجدداً.");

    // أخطاء السائقين
    public static readonly ApiError DriverNotFound = new(
        "DRIVER_NOT_FOUND",
        "السائق المطلوب (رقم {0}) غير موجود.",
        "تأكد من صحة معرف السائق المختار.");

    public static readonly ApiError DriverCreateFailed = new(
        "DRIVER_CREATE_FAILED",
        "فشل إنشاء حساب السائق: {0}",
        "تأكد من صحة البيانات (البريد أو الهاتف قد يكون مستخدماً).");

    public static readonly ApiError DriverUpdateFailed = new(
        "DRIVER_UPDATE_FAILED",
        "فشل تحديث بيانات السائق (رقم {0}): {1}",
        "تأكد من صحة البيانات المدخلة.");

    public static readonly ApiError DriverDeleteFailed = new(
        "DRIVER_DELETE_FAILED",
        "فشل حذف السائق (رقم {0}): {1}",
        "قد لا يمكن حذف السائق إذا كان مرتبطاً بطلبات حالية.");

    // أخطاء المستخدمين
    public static readonly ApiError UserNotFound = new(
        "USER_NOT_FOUND",
        "المستخدم المطلوب (رقم {0}) غير موجود.",
        "تأكد من صحة معرف المستخدم المختار.");

    public static readonly ApiError UserCreateFailed = new(
        "USER_CREATE_FAILED",
        "فشل إنشاء حساب المستخدم: {0}",
        "تأكد من البيانات المدخلة (البريد أو الهاتف قد يكون مستخدماً).");

    public static readonly ApiError UserUpdateFailed = new(
        "USER_UPDATE_FAILED",
        "فشل تحديث بيانات المستخدم (رقم {0}): {1}",
        "تأكد من صحة البيانات المدخلة.");

    // أخطاء أنواع الخرسانة
    public static readonly ApiError ConcreteTypeNotFound = new(
        "CONCRETE_TYPE_NOT_FOUND",
        "نوع الخرسانة المطلوب (رقم {0}) غير موجود.",
        "تأكد من صحة المعرّف المختار.");

    public static readonly ApiError ConcreteTypeCreateFailed = new(
        "CONCRETE_TYPE_CREATE_FAILED",
        "فشل إنشاء نوع الخرسانة: {0}",
        "تأكد من عدم تكرار الاسم أو البيانات المدخلة.");

    public static readonly ApiError ConcreteTypeUpdateFailed = new(
        "CONCRETE_TYPE_UPDATE_FAILED",
        "فشل تحديث نوع الخرسانة (رقم {0}): {1}",
        "تأكد من صحة البيانات.");

    public static readonly ApiError ConcreteTypeDeleteFailed = new(
        "CONCRETE_TYPE_DELETE_FAILED",
        "فشل حذف نوع الخرسانة (رقم {0}): {1}",
        "قد يكون هذا النوع مرتبطاً بطلبات قائمة.");
}
