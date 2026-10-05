namespace Kharasana.Application.Common;

public static class Messages
{
    #region Success Messages

    public const string RegisterSuccess = "تم إنشاء الحساب بنجاح.";
    public const string LoginSuccess = "تم تسجيل الدخول بنجاح.";
    public const string CreatedSuccessfully = "تم إنشاء البيانات بنجاح.";
    public const string UpdatedSuccessfully = "تم تحديث البيانات بنجاح.";
    public const string DeletedSuccessfully = "تم حذف البيانات بنجاح.";
    public const string DriverAssignedSuccessfully = "تم تعيين السائق بنجاح.";
    public const string OrderCreatedSuccess = "تم إنشاء الطلب بنجاح.";

    public const string UsersRetrievedSuccessfully = "تم جلب المستخدمين بنجاح.";
    public const string UserRetrievedSuccessfully = "تم جلب المستخدم بنجاح.";

    public const string OrdersRetrievedSuccessfully = "تم جلب جميع الطلبات بنجاح.";
    public const string OrderRetrievedSuccessfully = "تم جلب تفاصيل الطلب بنجاح.";
    public const string OrderStatusUpdated = "تم تحديث حالة الطلب بنجاح.";

    public const string FactoriesRetrievedSuccessfully = "تم جلب المصانع بنجاح.";
    public const string FactoryRetrievedSuccessfully = "تم جلب المصنع بنجاح.";
    public const string ArchivedFactoriesRetrievedSuccessfully = "تم جلب المصانع المؤرشفة بنجاح.";
    public const string FactoryRestoredSuccessfully = "تم استعادة المصنع بنجاح.";
    public const string ConcreteTypeRestoredSuccessfully = "تم استعادة نوع الخرسانة بنجاح.";
    public const string ArchivedConcreteTypesRetrievedSuccessfully = "تم جلب أنواع الخرسانة المؤرشفة بنجاح.";
    public const string FactoryLogoUploadedSuccessfully = "تم رفع شعار المصنع بنجاح.";
    public const string FactoryLogoDeletedSuccessfully = "تم حذف شعار المصنع بنجاح.";
    public const string FactorySettingsRetrievedSuccessfully = "تم جلب بيانات المصنع بنجاح.";
    public const string DriverActivatedSuccessfully = "تم تفعيل حساب السائق بنجاح.";
    public const string DriverDeactivatedSuccessfully = "تم إيقاف حساب السائق بنجاح.";

    public const string ConcreteTypesRetrievedSuccessfully = "تم جلب أنواع الخرسانة بنجاح.";
    public const string ConcreteTypeRetrievedSuccessfully = "تم جلب نوع الخرسانة بنجاح.";

    public const string DashboardRetrievedSuccessfully = "تم جلب بيانات لوحة التحكم بنجاح.";
    public const string ReportsRetrievedSuccessfully = "تم جلب التقارير بنجاح.";

    public const string OrderUpdatedSuccessfully = "تم تحديث الطلب بنجاح.";
    public const string PriceSavedSuccessfully = "تم حفظ السعر بنجاح.";
    public const string OrderApprovedSuccessfully = "تم اعتماد الطلب بنجاح.";
    public const string DeliveryStartedSuccessfully = "تم بدء التوصيل بنجاح.";
    public const string OrderDeliveredSuccessfully = "تم تسليم الطلب بنجاح.";
    public const string OrderClosedSuccessfully = "تم إغلاق الطلب بنجاح.";
    public const string OrderRejectedSuccessfully = "تم رفض الطلب بنجاح.";
    public const string OrderCancelledSuccessfully = "تم إلغاء الطلب بنجاح.";
    public const string OrderArchivedSuccessfully = "تم أرشفة الطلب بنجاح.";
    public const string OrderDeletedSuccessfully = "تم حذف الطلب بنجاح.";

    #endregion

    #region Not Found Messages

    public const string UserNotFound = "المستخدم المحدد غير موجود.";
    public const string FactoryNotFound = "المصنع المحدد غير موجود.";
    public const string ConcreteTypeNotFound = "نوع الخرسانة المحدد غير موجود.";
    public const string OrderNotFound = "الطلب المحدد غير موجود.";
    public const string DriverNotFound = "السائق المحدد غير موجود.";

    #endregion

    #region Validation Messages

    public const string EmailAlreadyExists = "البريد الإلكتروني مستخدم مسبقًا.";
    public const string InvalidEmail = "البريد الإلكتروني غير صالح.";
    public const string EmailMaxLength = "البريد الإلكتروني يجب ألا يزيد عن 256 حرفاً.";
    public const string PhoneAlreadyExists = "رقم الهاتف مستخدم مسبقًا.";
    public const string EmailOrPhoneRequired = "يجب إدخال بريد إلكتروني أو رقم هاتف على الأقل.";
    public const string FactoryAlreadyExists = "اسم المصنع مستخدم مسبقًا.";
    public const string FactoryAlreadyHasAccount = "هذا المصنع يمتلك حساباً مسجلاً بالفعل ولا يمكن إنشاء حساب آخر له.";
    public const string ConcreteTypeAlreadyExists = "نوع الخرسانة موجود مسبقًا لهذا المصنع.";

    /// <summary>فشل استعادة نوع خرسانة لأن الاسم نفسه ما زال مستخدمًا بنوع نشط في المصنع.</summary>
    public const string ConcreteTypeRestoreNameConflict = "لا يمكن استعادة نوع الخرسانة: الاسم \"{0}\" مستخدم بالفعل في هذا المصنع. يجب إعادة تسمية النوع الحالي أو حذفه قبل الاستعادة.";

    public const string PasswordsNotMatch = "كلمة المرور وتأكيد كلمة المرور غير متطابقتين.";
    public const string InvalidCredentials = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
    public const string AccountLocked = "تم قفل الحساب بسبب محاولات دخول فاشلة متكررة. حاول مرة أخرى بعد 15 دقيقة.";
    public const string Unauthorized = "ليس لديك صلاحية لتنفيذ هذه العملية.";
    public const string InvalidOrExpiredToken = "التوكن غير موجود أو غير صالح أو منتهي الصلاحية.";
    public const string InvalidDriver = "المستخدم المحدد ليس سائقًا.";
    public const string InvalidClient = "المستخدم المحدد ليس عميلاً.";
    public const string InvalidFactory = "المصنع المحدد غير صالح.";
    public const string InvalidConcreteType = "نوع الخرسانة المحدد غير صالح.";
    public const string FactoryMismatch = "نوع الخرسانة لا يتبع المصنع المحدد.";
    public const string InvalidStatusTransition = "لا يمكن الانتقال إلى الحالة المطلوبة.";
    public const string InvalidRegistrationRole = "يمكن للعملاء فقط إنشاء حسابات بأنفسهم.";

    // Factory/Employee/Drivers status messages
    public const string FactoryArchived = "لم يعد بإمكانك الدخول إلى الحساب. راجع الدعم نظراً لأن حسابك موقف من قبل الإدارة.";
    public const string FactoryInactive = "مصنعك غير نشط حالياً. لا يمكنك إضافة أنواع خرسانة جديدة.";
    public const string FactoryInactiveLogin = "تنبيه: مصنعك غير نشط. بعض الميزات قد لا تكون متاحة.";
    public const string DriverDeactivated = "حسابك غير نشط. راجع موظف المصنع لتفعيل حسابك.";
    public const string UserInactive = "الحساب غير نشط.";

    public const string InvalidLogoFile = "يرجى اختيار ملف صورة صالح.";
    public const string FactoryHasNoLogoToDelete = "لا يوجد شعار لهذا المصنع لحذفه.";

    // تخزين الملفات (ImageStorageService) — رسائل تصل للمستخدم عبر ردّ الـ API
    public const string InvalidUploadedFile = "الملف المرفوع غير صالح.";
    public const string UnsupportedFileExtension = "امتداد الملف غير مدعوم. الامتدادات المسموحة: jpg, jpeg, png, webp.";
    public const string FileTooLarge = "حجم الملف يتجاوز الحد المسموح به (5 ميجابايت).";
    public const string InvalidFileContentType = "نوع محتوى الملف غير صالح أو لا يتطابق مع الامتداد.";
    public const string InvalidStorageFolder = "مجلد تخزين الصور غير صالح.";
    public const string FactoryNotFoundOrInactive = "المصنع غير موجود أو غير نشط.";
    public const string ConcreteTypeNotFoundShort = "نوع الخرسانة غير موجود.";
    public const string ConcreteTypeNotActiveForClient = "نوع الخرسانة غير نشط.";
    public const string ConcreteTypeNotFoundOrNotForFactory = "نوع الخرسانة غير موجود أو لا ينتمي لمصنعك.";

    public const string FactoryRequiredForDriver = "يجب تحديد المصنع للسائق.";
    public const string FactoryRequiredForEmployee = "يجب تحديد المصنع للموظف.";
    public const string CannotCreateAdmin = "لا يمكن إنشاء حساب Admin عبر الـ API.";
    public const string CannotChangeToAdmin = "لا يمكن تغيير دور المستخدم إلى Admin عبر الـ API.";
    public const string CannotDeleteAdmin = "لا يمكن حذف حساب Admin.";

    public const string ClientRequiredForAdminOrder = "يجب تحديد العميل عند إنشاء الطلب نيابة عنه.";
    public const string OrderCreationNotAllowed = "غير مسموح لك بإنشاء الطلبات.";
    public const string DriverAssignmentOnlyForApprovedOrders = "لا يمكن تعيين سائق إلا للطلبات المعتمدة (Approved).";
    public const string CannotAssignDriverForClientTransport = "لا يمكن تعيين سائق لطلب اختار فيه العميل وسيلة نقل خاصة.";
    public const string FactoryEmployeeFactoryMismatch = "لا يمكنك تنفيذ هذه العملية لمصنع آخر غير مصنعك.";
    public const string ClientFullNameRequiredForNewClient = "الاسم الكامل مطلوب عند إنشاء عميل جديد بالهاتف.";

    public const string UserIdNotFound = "لم يتم العثور على هوية المستخدم.";
    public const string UserRoleNotFound = "لم يتم العثور على دور المستخدم.";
    public const string FactoryNotFoundForUser = "لم يتم العثور على المصنع المرتبط بالمستخدم.";

    #endregion

    #region Yemeni Phone Validation

    public const string InvalidYemeniPhone = "رقم الهاتف غير صالح. يجب أن يكون رقم يمني صحيح (مثال: 7XXXXXXXX).";
    public const string InvalidWhatsAppNumber = "رقم الواتساب غير صالح. يجب أن يكون رقم يمني صحيح (مثال: 7XXXXXXXX).";
    public const string PasswordMinLength = "كلمة المرور يجب أن تكون 8 أحرف على الأقل.";
    public const string NameMaxLength = "الاسم الكامل يجب ألا يزيد عن 200 حرف.";
    public const string FullNameRequired = "الاسم الكامل مطلوب.";
    public const string PasswordRequired = "كلمة المرور مطلوبة.";
    public const string ConfirmPasswordRequired = "تأكيد كلمة المرور مطلوب.";

    #endregion

    #region Factory / ConcreteType / User Validation Messages

    // المصنع
    public const string FactoryNameRequired = "اسم المصنع مطلوب.";
    public const string FactoryNameMaxLength = "اسم المصنع يجب ألا يزيد عن 200 حرف.";
    public const string FactoryAreaRequired = "المنطقة مطلوبة.";

    /// <summary>
    /// نفس نصّ <see cref="SiteAreaMaxLength"/> عمداً، لكنه منفصلٌ عنه: صياغة منطقة
    /// الموقع ورسالة منطقة المصنع حقلان مستقلان، فلا يجوز أن يغيّر أحدهما الآخر.
    /// </summary>
    public const string FactoryAreaMaxLength = "المنطقة يجب ألا تزيد عن 100 حرف.";
    public const string FactoryAddressRequired = "العنوان مطلوب.";
    public const string FactoryAddressMaxLength = "العنوان يجب ألا يزيد عن 300 حرف.";
    public const string FactoryOwnerNameMaxLength = "اسم المالك يجب ألا يزيد عن 200 حرف.";

    // نوع الخرسانة
    public const string ConcreteTypeNameRequired = "اسم نوع الخرسانة مطلوب.";
    public const string ConcreteTypeNameMaxLength = "اسم نوع الخرسانة يجب ألا يزيد عن 100 حرف.";
    public const string ConcreteTypeStrengthInvalid = "قيمة المقاومة غير صالحة.";
    public const string ConcreteTypePriceMustBePositive = "السعر يجب أن يكون أكبر من صفر.";

    // المستخدم والسائق
    public const string InvalidRole = "الدور غير صالح.";
    public const string InvalidOrderStatus = "حالة الطلب غير صالحة.";
    public const string InvalidDriverStatus = "حالة السائق غير صالحة.";
    public const string LoginIdentifierRequired = "يجب إدخال البريد الإلكتروني أو رقم الهاتف.";
    public const string DriverRequired = "يجب تحديد السائق.";
    public const string ClientPhoneRequired = "رقم هاتف العميل مطلوب.";
    public const string TruckPlateRequired = "رقم لوحة الشاحنة مطلوب.";
    public const string TruckPlateMaxLength = "رقم لوحة الشاحنة يجب ألا يزيد عن 30 حرفًا.";

    #endregion

    #region Driver Status Messages

    public const string DriverNotAvailable = "السائق المحدد غير متاح حاليًا (مشغول أو غير متصل).";
    public const string DriverStatusUpdatedSuccessfully = "تم تحديث حالة السائق بنجاح.";

    #endregion

    #region Order Validation Messages

    public const string ConcurrencyConflict = "تم تعديل هذا الطلب من قبل مستخدم آخر. الرجاء إعادة تحميل الصفحة والمحاولة مرة أخرى.";
    public const string OrderCannotBeUpdatedInStatus = "لا يمكن تعديل الطلب في حالته الحالية \"{0}\". يُسمح بالتعديل فقط للطلبات قيد الانتظار.";
    public const string OrderCannotBeRejectedInStatus = "لا يمكن رفض هذا الطلب في حالته الحالية.";
    public const string ConcreteTypeNotFoundById = "نوع الخرسانة المحدد (ID: {0}) غير موجود في النظام.";
    public const string ConcreteTypeNotBelongsToFactory = "نوع الخرسانة المحدد لا ينتمي إلى مصنعك الحالي. يرجى اختيار نوع خرسانة تابع للمصنع.";
    public const string ConcreteTypeInactiveForOrder = "نوع الخرسانة \"{0}\" غير نشط حالياً. يرجى اختيار نوع خرسانة نشط.";
    public const string ConcreteTypeInactive = "نوع الخرسانة غير نشط. يرجى اختيار نوع آخر.";
    public const string ConcreteTypeRequired = "يجب تحديد نوع الخرسانة.";
    public const string FactoryRequired = "يجب تحديد المصنع.";
    public const string TransportMethodInvalid = "طريقة النقل غير صالحة.";
    public const string SlabTypeInvalid = "نوع الصبة غير صالح.";
    public const string QuantityMustBePositive = "الكمية يجب أن تكون أكبر من صفر.";
    public const string QuantityTooLarge = "الكمية كبيرة جداً (أقصى حد هو 1000 متر مكعب). يرجى التواصل مع الدعم إذا كانت الكمية أكبر.";

    // ✅ سقوف النصوص الحرة — كل واحد يطابق HasMaxLength للعمود في قاعدة البيانات.
    //    بدونها يمرّ النص الأطول إلى SQL Server فيرمي الخطأ 8152 (اقتطاع) فيصير
    //    الرد 500 ويُفقد الطلب كاملاً بسبب حرف زائد في الملاحظات.
    public const string ProjectNameMaxLength = "اسم المشروع يجب ألا يزيد عن 200 حرف.";
    public const string ProjectOwnerNameMaxLength = "اسم صاحب المشروع يجب ألا يزيد عن 200 حرف.";
    public const string SiteAreaMaxLength = "المنطقة يجب ألا تزيد عن 100 حرف.";
    public const string SiteDescriptionMaxLength = "وصف الموقع يجب ألا يزيد عن 500 حرف.";
    public const string NotesMaxLength = "الملاحظات يجب ألا تزيد عن 1000 حرف.";
    public const string PouringDateCannotBeInPast = "تاريخ الصب لا يمكن أن يكون في الماضي. يرجى اختيار تاريخ اليوم أو تاريخ مستقبلي.";
    public const string PumpRequiresFloorNumber = "عند اختيار مضخة، يجب تحديد رقم الطابق.";
    public const string FloorNumberMustBeNonNegative = "رقم الطابق يجب أن يكون صفر أو أكبر (الأرضي = 0).";
    public const string RejectionReasonPrefix = "سبب الرفض: {0}";
    public const string RejectionReasonMaxLength = "سبب الرفض لا يجب أن يتجاوز 500 حرف.";
    public const string UnitPriceMustBePositive = "سعر المتر يجب أن يكون أكبر من صفر.";
    public const string CannotUpdatePriceAtThisStage = "لا يمكن تعديل السعر في هذه المرحلة من الطلب.";
    public const string NotAuthorizedToApproveOrder = "غير مخول لاعتماد الطلب.";
    public const string NotAuthorizedToViewReport = "غير مخول لعرض تقرير سائق خارج نطاقك.";
    public const string CannotApproveNonPendingOrder = "لا يمكن اعتماد الطلب إلا وهو في حالة قيد الانتظار.";
    public const string PriceRequiredBeforeApproval = "يجب تحديد سعر المتر قبل الاعتماد.";
    public const string OrderNotAssignedForStartDelivery = "هذا الطلب غير مسند إليك. لا يمكنك بدء التوصيل.";
    public const string OrderNotAssignedForDeliver = "هذا الطلب غير مسند إليك. لا يمكنك تأكيد التسليم.";
    public const string OrderNotAssignedForCancel = "هذا الطلب غير مسند إليك. لا يمكنك إلغاؤه.";
    public const string CannotStartDeliveryForNonApprovedOrder = "لا يمكن بدء التوصيل إلا لطلب معتمد.";
    public const string DriverRequiredForDelivery = "يجب تعيين سائق قبل بدء التوصيل.";
    public const string CannotDeliverNonOnTheWayOrder = "لا يمكن تأكيد التسليم إلا لطلب في الطريق.";
    public const string CannotCloseNonDeliveredOrder = "لا يمكن إغلاق الطلب إلا بعد تسليمه.";
    public const string CannotCancelDeliveredOrClosedOrder = "لا يمكن إلغاء طلب تم تسليمه أو إغلاقه.";
    public const string OrderAlreadyRejectedOrCancelled = "الطلب مرفوض أو ملغي بالفعل.";
    public const string CannotChangeDriverRole = "لا يمكن تغيير دور السائق.";
    public const string PhoneLinkedToNonClientAccount = "رقم الهاتف مرتبط بحساب موظف/سائق موجود في النظام. لا يمكن إنشاء عميل جديد بنفس الرقم. يرجى التحقق من الرقم أو التواصل مع مدير النظام.";

    #endregion

    #region General Messages

    public const string UnexpectedError = "حدث خطأ غير متوقع.";

    /// <summary>
    /// احتياط أخير لفشل ModelState لا يحمل رسالة عربية مكتوبة (جسم JSON مشوّه،
    /// أو قيمة لا تُحوَّل إلى نوع الحقل). تُستخدم بدل تسريب رسالة المحلّل الإنجليزية
    /// («The JSON value could not be converted to…») إلى المستخدم.
    /// </summary>
    public const string InvalidRequest = "البيانات المُرسَلة غير صالحة. تحقق من الحقول ثم أعد المحاولة.";

    /// <summary>
    /// احتياط أخير لانتهاك قيد التفرّد على مستوى قاعدة البيانات (حالة سباق فاتت الفحص المسبق).
    /// قاعدة البيانات هي المرجع النهائي، لذا تُترجم هذه الحالة إلى 409 بدل 500.
    /// </summary>
    public const string DuplicateValueConflict = "لا يمكن حفظ البيانات: إحدى القيم المدخلة مستخدمة مسبقًا (اسم مكرر أو بريد إلكتروني أو رقم هاتف).";

    public const string CustomersRetrievedSuccessfully = "تم جلب العملاء بنجاح.";

    /// <summary>لا توجد طلبات لهذا العميل في النطاق المطلوب (ومنها تُبنى إحصاءاته كلها).</summary>
    public const string CustomerNotFound = "لا توجد طلبات لهذا العميل.";

    /// <summary>حصر نطاق أنواع الخرسانة على مصنع المستخدم — مسار الإنشاء وحده؛
    /// القراءة والتعديل والحذف والاستعادة تستعمل <see cref="FactoryEmployeeFactoryMismatch"/> العامة.</summary>
    public const string CannotCreateConcreteTypeForOtherFactory = "لا يمكنك إنشاء نوع خرسانة لمصنع آخر.";

    #endregion

    #region Fallback Display Strings

    public const string ClientFallback = "العميل #{0}";
    public const string FactoryFallback = "المصنع #{0}";
    public const string ConcreteTypeFallback = "نوع الخرسانة #{0}";

    #endregion
}
