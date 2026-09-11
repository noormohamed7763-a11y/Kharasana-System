namespace Kharasana.Web.Localization;

/// <summary>
/// كتالوج موحّد لكل رسائل التطبيق باللغة العربية.
/// الغرض: أن تعيش كل رسالة في مكان واحد، فيتّسق النص عبر الشاشات،
/// ويُسهَّل ترجمتها أو تعديلها مستقبلاً دون البحث في كل الملفات.
/// تُقسَّم إلى مجموعات: رسائل نجاح / أخطاء / تحذيرات / تنبيهات / تحقق.
/// </summary>
public static class AppMessages
{
    /// <summary>رسائل عامة مشتركة بين كل الوحدات.</summary>
    public static class Common
    {
        public const string Ok = "تمت العملية بنجاح.";
        public const string OperationFailed = "تعذر تنفيذ العملية. حاول مرة أخرى.";
        public const string NetworkError = "تعذر الاتصال بالخادم. تحقق من اتصالك بالإنترنت وحاول مرة أخرى.";
        public const string ServerError = "حدث خطأ غير متوقع في الخادم. جرّب مرة أخرى لاحقاً.";
        public const string NotFound = "العنصر المطلوب غير موجود.";
        public const string Unauthorized = "انتهت جلستك أو أنك غير مسجّل الدخول. يرجى تسجيل الدخول مرة أخرى.";
        public const string Forbidden = "عذراً، ليس لديك صلاحية للوصول إلى هذه الصفحة.";
        public const string InvalidData = "البيانات المدخلة غير صحيحة.";
        public const string NoItems = "لا توجد عناصر لعرضها.";
        public const string TooManyAttempts = "محاولات كثيرة. حاول مرة أخرى بعد قليل.";
    }

    /// <summary>رسائل النجاح.</summary>
    public static class Success
    {
        public const string Saved = "تم الحفظ بنجاح.";
        public const string Created = "تم الإنشاء بنجاح.";
        public const string Updated = "تم التعديل بنجاح.";
        public const string Deleted = "تم الحذف بنجاح.";
        public const string Archived = "تمت الأرشفة بنجاح.";
        public const string Restored = "تمت الاستعادة بنجاح.";
        public const string LogoUpdated = "تم تحديث الشعار بنجاح.";
        public const string LogoDeleted = "تم حذف الشعار بنجاح.";
        public const string AccountCreated = "تم إنشاء الحساب بنجاح.";
        public const string StatusUpdated = "تم تحديث الحالة بنجاح.";
        public const string DriverAssigned = "تم تعيين السائق بنجاح.";
        public const string PriceSaved = "تم حفظ السعر بنجاح.";
    }

    /// <summary>رسائل الأخطاء.</summary>
    public static class Error
    {
        public const string Created = "تعذر الإنشاء. حاول مرة أخرى.";
        public const string Updated = "تعذر التعديل. حاول مرة أخرى.";
        public const string Deleted = "تعذر الحذف. حاول مرة أخرى.";
        public const string Archived = "تعذرت الأرشفة. حاول مرة أخرى.";
        public const string Restored = "تعذرت الاستعادة. حاول مرة أخرى.";
        public const string Saved = "تعذر الحفظ. حاول مرة أخرى.";
        public const string LogoUpload = "تعذر رفع الشعار. تأكد من اختيار صورة صالحة.";
        public const string LogoDelete = "تعذر حذف الشعار.";
        public const string AccountCreate = "تعذر إنشاء الحساب.";
        public const string StatusUpdate = "تعذر تحديث الحالة.";
        public const string DriverAssign = "تعذر تعيين السائق.";
        public const string PriceSave = "تعذر حفظ السعر.";
        public const string InvalidLogin = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
        public const string InvalidLogo = "يرجى اختيار صورة صالحة.";
        public const string InvalidId = "المعرّف المطلوب غير صحيح.";
    }

    /// <summary>رسائل التحقق من صحة الإدخال.</summary>
    public static class Validation
    {
        public const string RequiredField = "هذا الحقل مطلوب.";
        public const string InvalidPhone = "يرجى إدخال رقم هاتف صحيح.";
        public const string InvalidEmail = "يرجى إدخال بريد إلكتروني صحيح.";
        public const string InvalidRange = "القيمة المدخلة خارج النطاق المسموح.";
        public const string InvalidStatus = "الحالة غير صالحة.";
    }
}
