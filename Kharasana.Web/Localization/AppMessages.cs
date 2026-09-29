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

        /// <summary>
        /// صيغة مختصرة من <see cref="Forbidden"/> تُعاد في ردود AJAX (BadRequest/Ok)
        /// التي يعرضها الـ JS مباشرةً بلا صفحة كاملة. أُبقيت منفصلة حرفيًّا.
        /// </summary>
        public const string PermissionDeniedShort = "ليس لديك صلاحية.";
        public const string InvalidData = "البيانات المدخلة غير صحيحة.";
        public const string NoItems = "لا توجد عناصر لعرضها.";
        public const string TooManyAttempts = "محاولات كثيرة. حاول مرة أخرى بعد قليل.";

        /// <summary>ردّ 409 من الـ API عندما تتعارض البيانات مع سجل قائم.</summary>
        public const string DataConflict = "البيانات مستخدمة مسبقاً أو متعارضة.";

        /// <summary>
        /// ردّ 429 من الـ API — تجاوز حدّ الطلبات من الجهاز نفسه. تختلف صياغتها عن
        /// <see cref="TooManyAttempts"/> (محاولات تسجيل الدخول) فتُبقى كلتاهما حرفيًّا.
        /// </summary>
        public const string DeviceRateLimited = "طلبات كثيرة من جهازك. انتظر قليلاً ثم أعد المحاولة.";

        /// <summary>انتهت مهلة الاتصال بالنظام قبل وصول ردّ الـ API.</summary>
        public const string RequestTimeout = "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.";
        public const string OrderCannotModify = "لا تملك صلاحية تعديل هذا الطلب.";
        public const string OrderCannotChangeStatus = "لا تملك صلاحية تغيير حالة هذا الطلب.";
        public const string DriverNotInFactory = "السائق المحدد لا يعمل في مصنعك أو غير متاح.";
        public const string OrderCannotUpdateInStatus = "لا يمكن تعديل الطلب في حالته الحالية.";

        /// <summary>تعذّر الوصول إلى الـ API أصلًا (ردّ فارغ) — لا خطأ من الخادم.</summary>
        public const string MainServerUnreachable = "تعذر الاتصال بالخادم الرئيسي. يرجى المحاولة مرة أخرى.";

        /// <summary>فشل غير مصنَّف في طبقة الويب.</summary>
        public const string UnexpectedError = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";

        /// <summary>
        /// ⚠️ نصّ موجّه للمطوّر لا للمستخدم النهائي — يظهر في شاشة المستخدم عند فشل
        /// الاتصال بالـ API. نُقل إلى الكتالوج <b>بحرفه</b> عند توحيد نصوص
        /// <c>UserApiService</c>؛ تغيير صياغته تغييرٌ لنصّ يراه المستخدم فيحتاج قرارًا
        /// مستقلًا، فلا تُعِد صياغته من تلقاء نفسك.
        /// </summary>
        public const string ApiNotRunning = "خطأ في الاتصال بالخادم. تأكد من تشغيل الـ API.";

        // ——— صلاحيات وقيود خاصة بكل وحدة (تختلف صياغتها عن Common.Forbidden فتُبقى كما هي)

        /// <summary>تقرير السائق يُفتح من بطاقة سائق لا من بطاقة أي مستخدم آخر.</summary>
        public const string DriverReportForDriverCardOnly = "التقرير متاح لبطاقة سائق فقط.";

        public const string DriversViewForbidden = "ليس لديك صلاحية لعرض بيانات السائقين.";
        public const string DriverEditForbidden = "ليس لديك صلاحية لتعديل السائقين.";
        public const string DriverDeleteForbidden = "ليس لديك صلاحية لحذف السائقين.";
        public const string DriverStatusForbidden = "ليس لديك صلاحية لتحديث حالة السائقين.";
        public const string DriverCreateFactoryEmployeeOnly = "إضافة السائقين متاحة حاليًا لموظف المصنع فقط.";

        /// <summary>الدوران الوحيدان اللذان يلزمهما مصنع مرتبط بالحساب.</summary>
        public const string FactoryRequiredForDriverAndEmployee = "المصنع مطلوب للسائق وموظف المصنع.";

        // مصنع غير نشط: منع إضافة/تعديل/حذف أنواع الخرسانة — ثلاث رسائل بثلاث صيغ مقصودة.
        public const string FactoryInactiveCannotAddConcreteTypes = "مصنعك غير نشط حالياً، لذا لا يمكنك إضافة أنواع خرسانة.";
        public const string FactoryInactiveCannotEditConcreteTypes = "مصنعك غير نشط حالياً، لذا لا يمكنك تعديل أنواع الخرسانة.";
        public const string FactoryInactiveCannotDeleteConcreteTypes = "مصنعك غير نشط حالياً، لذا لا يمكنك حذف أنواع الخرسانة.";
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

        /// <summary>
        /// نصّ <c>SettingsController</c> الخاص بصفحة إعدادات المصنع. يختلف لفظًا عن
        /// <see cref="LogoUpdated"/> — أُبقيا معًا حرفيًّا عند التوحيد حتى لا يتغيّر
        /// نصٌّ يراه المستخدم؛ دمجُهما قرارٌ مستقلّ.
        /// </summary>
        public const string FactoryLogoUpdated = "تم تحديث شعار المصنع بنجاح.";
        public const string LogoDeleted = "تم حذف الشعار بنجاح.";
        public const string AccountCreated = "تم إنشاء الحساب بنجاح.";
        public const string StatusUpdated = "تم تحديث الحالة بنجاح.";
        public const string DriverAssigned = "تم تعيين السائق بنجاح.";
        public const string PriceSaved = "تم حفظ السعر بنجاح.";
        public const string Approved = "تم اعتماد الطلب بنجاح.";
        public const string Rejected = "تم رفض الطلب بنجاح.";
        public const string Cancelled = "تم إلغاء الطلب بنجاح.";
        public const string DeliveryStarted = "تم بدء التوصيل بنجاح.";
        public const string Delivered = "تم تسليم الطلب بنجاح.";
        public const string Closed = "تم إغلاق الطلب بنجاح.";

        /// <summary>
        /// تُعرض عند إنشاء طلب هاتفي لعميل غير مسجَّل، فيُنشأ له حساب بكلمة مرور مؤقتة.
        /// {0} = رقم هاتف العميل، {1} = كلمة المرور المؤقتة.
        /// ⚠️ تُعرض مرة واحدة فقط — لا تُخزَّن نصاً صريحاً ولا تُعاد لاحقاً.
        /// </summary>
        public const string NewClientAccountCreated =
            "تم إنشاء الطلب وإنشاء حساب للعميل. سلّم العميل البيانات التالية الآن — لن تُعرض كلمة المرور مرة أخرى: " +
            "الهاتف: {0} | كلمة المرور المؤقتة: {1}";

        // ——— المستخدمون
        public const string UserCreated = "تم إنشاء المستخدم بنجاح.";
        public const string UserUpdated = "تم تحديث المستخدم بنجاح.";
        public const string UserDeleted = "تم حذف المستخدم بنجاح.";

        // ——— السائقون
        public const string DriverAccountCreated = "تم إنشاء حساب السائق بنجاح.";
        public const string DriverUpdated = "تم تعديل بيانات السائق بنجاح.";
        public const string DriverDeleted = "تم حذف السائق بنجاح.";
        public const string DriverStatusUpdated = "تم تحديث حالة السائق بنجاح.";

        /// <summary>صيغة AJAX مختصرة (بلا «بنجاح») تُعاد لطلب حفظ الحالة من الصفحة.</summary>
        public const string DriverStatusUpdatedShort = "تم تحديث حالة السائق.";

        /// <summary>
        /// صيغة AJAX مختصرة لتفعيل/إيقاف حساب السائق (بلا «بنجاح») تُعاد للصفحة مباشرة.
        /// تختلف عن <c>Messages.DriverActivatedSuccessfully</c> في طبقة Application فتُبقى منفصلة.
        /// </summary>
        public const string DriverActivatedShort = "تم تفعيل حساب السائق.";
        public const string DriverDeactivatedShort = "تم إيقاف حساب السائق.";

        // ——— أنواع الخرسانة
        public const string ConcreteTypeCreated = "تم إنشاء نوع الخرسانة بنجاح.";
        public const string ConcreteTypeUpdated = "تم تحديث نوع الخرسانة بنجاح.";
        public const string ConcreteTypeDeleted = "تم حذف نوع الخرسانة بنجاح.";
        public const string ConcreteTypeRestored = "تم استعادة نوع الخرسانة بنجاح.";

        // ——— العملاء
        public const string ClientAccountCreated = "تم إنشاء حساب العميل بنجاح.";
        public const string ClientUpdated = "تم تعديل بيانات العميل بنجاح.";
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

        /// <summary>
        /// نصّ <c>SettingsController</c> المختصر؛ يختلف عن <see cref="LogoUpload"/> الأطول
        /// — أُبقيا معًا حرفيًّا (انظر <see cref="Success.FactoryLogoUpdated"/>).
        /// </summary>
        public const string LogoUploadShort = "تعذر رفع الشعار.";

        /// <summary>تعذّر جلب نموذج إعدادات المصنع من الـ API.</summary>
        public const string FactorySettingsLoad = "تعذر تحميل بيانات المصنع.";
        public const string LogoDelete = "تعذر حذف الشعار.";
        public const string AccountCreate = "تعذر إنشاء الحساب.";
        public const string StatusUpdate = "تعذر تحديث الحالة.";
        public const string DriverAssign = "تعذر تعيين السائق.";
        public const string PriceSave = "تعذر حفظ السعر.";
        public const string Approved = "تعذر اعتماد الطلب. حاول مرة أخرى.";
        public const string Rejected = "تعذر رفض الطلب. حاول مرة أخرى.";
        public const string Cancelled = "تعذر إلغاء الطلب. حاول مرة أخرى.";
        public const string DeliveryStarted = "تعذر بدء التوصيل. حاول مرة أخرى.";
        public const string Delivered = "تعذر تسجيل التسليم. حاول مرة أخرى.";
        public const string Closed = "تعذر إغلاق الطلب. حاول مرة أخرى.";
        public const string InvalidLogin = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
        public const string InvalidLogo = "يرجى اختيار صورة صالحة.";
        public const string InvalidId = "المعرّف المطلوب غير صحيح.";

        // ——— المستخدمون
        public const string UserCreate = "حدث خطأ أثناء إنشاء المستخدم.";
        public const string UserUpdate = "حدث خطأ أثناء تحديث المستخدم.";
        public const string UserDelete = "حدث خطأ أثناء حذف المستخدم.";

        // ——— السائقون
        public const string DriverCreate = "تعذر إنشاء حساب السائق لسبب غير معروف.";
        public const string DriverUpdate = "تعذر تعديل بيانات السائق لسبب غير معروف.";
        public const string DriverDelete = "تعذر حذف/تعطيل السائق. تأكد أنه غير مرتبط برحلات نشطة.";
        public const string DriverStatusUpdate = "تعذر تحديث حالة السائق.";

        /// <summary>صيغة AJAX مختصرة من <see cref="DriverStatusUpdate"/> تُعاد كـ 400 للصفحة.</summary>
        public const string DriverStatusUpdateShort = "تعذر تحديث الحالة.";

        // ——— أنواع الخرسانة
        public const string ConcreteTypeCreate = "تعذر إنشاء نوع الخرسانة.";
        public const string ConcreteTypeUpdate = "تعذر تحديث نوع الخرسانة.";
        public const string ConcreteTypeDelete = "تعذر حذف نوع الخرسانة.";
        public const string ConcreteTypeRestore = "تعذر استعادة نوع الخرسانة.";

        // ——— العملاء
        public const string ClientAccountCreate = "تعذر إنشاء حساب العميل. تأكد من عدم تكرار البريد أو الهاتف.";
        public const string ClientUpdate = "تعذر تعديل بيانات العميل.";

        // ——— التقارير
        public const string ReportsLoad = "تعذر تحميل بيانات التقارير.";
    }

    /// <summary>رسائل التحقق من صحة الإدخال.</summary>
    public static class Validation
    {
        public const string RequiredField = "هذا الحقل مطلوب.";
        public const string InvalidPhone = "يرجى إدخال رقم هاتف صحيح.";
        public const string InvalidEmail = "يرجى إدخال بريد إلكتروني صحيح.";
        public const string InvalidRange = "القيمة المدخلة خارج النطاق المسموح.";
        public const string InvalidStatus = "الحالة غير صالحة.";

        // ——— أنواع الخرسانة (نموذج الإنشاء/التعديل)
        public const string ConcreteTypeRequired = "يرجى اختيار نوع الخرسانة.";
        public const string CustomConcreteTypeNameRequired = "اسم النوع المخصص مطلوب.";
        public const string ConcreteStrengthRequired = "المقاومة مطلوبة.";
    }
}
