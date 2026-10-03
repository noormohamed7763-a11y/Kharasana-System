using FluentValidation;
using Kharasana.Application.Common;
using Kharasana.Domain.Common;
using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;

namespace Kharasana.Application.Validators.Common;

/// <summary>
/// قواعد تحقق مشتركة متعددة الاستخدام — توحّد الرسائل والسلوك عبر الفاحصين وتفادي التكرار.
///
/// <para><b>لماذا هذا الملف:</b> كانت القاعدة الواحدة تُكتب في عدة مُدقّقات، وبثلاثة
/// مصادر للرسالة: ثابت في <see cref="Messages"/> مرة، ونصّ عربي صريح مرة، وسقوط إلى
/// رسالة FluentValidation الإنجليزية مرة ثالثة. فانحرفت الحدود والرسائل بين مسارات كان
/// يجب أن تتطابق تماماً — وأخطرها سقف البريد (256) الذي كان حاضراً في مسار وغائباً عن
/// ثلاثة، فيمرّ النصّ إلى العمود ويرمي SQL Server الخطأ 8152 فيصير الرد 500 بدل 400.</para>
///
/// <para><b>قاعدة إلزامية:</b> كل سقف نصّي يمرّ من <see cref="CappedAt"/> أو
/// <see cref="RequiredCappedAt"/>. هاتان تفرضان رسالة عربية صريحة، فلا يمكن أن تسقط
/// رسالة إلى الافتراضية الإنجليزية بسبب ترتيب <c>WithMessage</c> بعد
/// <c>MaximumLength</c> — وهو الخطأ الذي كان قائماً في 11 موضعاً.</para>
/// </summary>
public static class ValidationRules
{
    // ============================================================
    // بوانٍ عامّة — كل قاعدة سقف/إلزام تمرّ من هنا
    // ============================================================

    /// <summary>سقف نصّي برسالة عربية إلزامية — يمنع السقوط إلى الرسالة الافتراضية.</summary>
    public static IRuleBuilderOptions<T, string?> CappedAt<T>(
        this IRuleBuilder<T, string?> builder, int maxLength, string message)
        => builder
            .MaximumLength(maxLength).WithMessage(message);

    /// <summary>مطلوب <b>و</b> مسقوف، برسالتين مختلفتين — لكل قاعدة رسالتها لا رسالة واحدة لهما.</summary>
    public static IRuleBuilderOptions<T, string?> RequiredCappedAt<T>(
        this IRuleBuilder<T, string?> builder, int maxLength, string requiredMessage, string maxLengthMessage)
        => builder
            .NotEmpty().WithMessage(requiredMessage)
            .MaximumLength(maxLength).WithMessage(maxLengthMessage);

    // ============================================================
    // الاسم والبريد والهاتف
    // ============================================================

    /// <summary>الاسم الكامل: مطلوب ولا يتجاوز 200 حرف.</summary>
    public static IRuleBuilderOptions<T, string?> FullName<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(200, Messages.FullNameRequired, Messages.NameMaxLength);

    /// <summary>كلمة المرور: مطلوبة وبالحد الأدنى الموحّد للطول (<see cref="PasswordPolicy.MinimumLength"/>).</summary>
    public static IRuleBuilderOptions<T, string?> Password<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .NotEmpty().WithMessage(Messages.PasswordRequired)
            .MinimumLength(PasswordPolicy.MinimumLength).WithMessage(Messages.PasswordMinLength);

    /// <summary>
    /// البريد الإلكتروني: صيغة صحيحة، وسقف 256 حرفاً يطابق <c>HasMaxLength</c> لعمود
    /// <c>Users.Email</c> وعمود <c>Factories.Email</c>.
    ///
    /// <para>السقف ليس تجميلاً: بدونه يمرّ نصّ أطول إلى العمود <c>nvarchar(256)</c>
    /// فيرمي SQL Server الخطأ 8152 (اقتطاع) فيصير الرد 500 بدل 400. كان السقف حاضراً
    /// في مُدقّق واحد وغائباً عن أربعة.</para>
    ///
    /// <para><b>اختياري بطبعه:</b> <c>MaximumLength</c> يتجاهل <c>null</c> ويمرّر النص
    /// الفارغ (طوله صفر)، لكن <c>EmailAddress</c> يرفض الفارغ — فالقاعدة تُشترط على
    /// غير الفارغ في موضع النداء بِـ <c>.When(x =&gt; !string.IsNullOrWhiteSpace(x.Email))</c>.</para>
    /// </summary>
    public static IRuleBuilderOptions<T, string?> Email<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .EmailAddress().WithMessage(Messages.InvalidEmail)
            .MaximumLength(256).WithMessage(Messages.EmailMaxLength);

    /// <summary>رقم هاتف يمني اختياري — يُقبل الفارغ.</summary>
    public static IRuleBuilderOptions<T, string?> YemeniPhone<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .Must(phone => string.IsNullOrWhiteSpace(phone) || YemeniPhoneHelper.IsValid(phone!))
            .WithMessage(Messages.InvalidYemeniPhone);

    /// <summary>رقم واتساب يمني اختياري — يُقبل الفارغ.</summary>
    public static IRuleBuilderOptions<T, string?> YemeniWhatsApp<T>(this IRuleBuilder<T, string?> builder)
        => builder
            .Must(whatsApp => string.IsNullOrWhiteSpace(whatsApp) || YemeniPhoneHelper.IsValid(whatsApp!))
            .WithMessage(Messages.InvalidWhatsAppNumber);

    // ============================================================
    // المصنع
    // ============================================================

    /// <summary>اسم المصنع: مطلوب وسقفه 200 (FactoryConfiguration.FactoryName).</summary>
    public static IRuleBuilderOptions<T, string?> FactoryName<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(200, Messages.FactoryNameRequired, Messages.FactoryNameMaxLength);

    /// <summary>منطقة المصنع: مطلوبة وسقفها 100 (FactoryConfiguration.Area).</summary>
    public static IRuleBuilderOptions<T, string?> FactoryArea<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(100, Messages.FactoryAreaRequired, Messages.FactoryAreaMaxLength);

    /// <summary>
    /// عنوان المصنع: مطلوب وسقفه 300.
    /// <b>سقف مزدوج مقصود (بقرار 2026-10-03):</b> العمود
    /// (<c>FactoryConfiguration.Address</c>) يسع 500 والسقف هنا 300،
    /// ولا يُوحَّدان بقصد. المُدقّق هو خط الدفاع الأول برسالة ودّية
    /// (300 حرف عربي ≈ 150 كلمة — أكثر من أي عنوان مصنع عمليًا)،
    /// والعمود 500 سعة احتياطية لا تُصل إليها المسارات الرسمية.
    /// تضييق العمود إلى 300 يحتاج <c>ALTER COLUMN</c> يفشل إن وُجد
    /// عنوان أطول في بيانات تشغيلية — والاتصال غير متاح هنا لفحصها
    /// — فالمكسب الصفري لا يبرّر المخاطرة.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> FactoryAddress<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(300, Messages.FactoryAddressRequired, Messages.FactoryAddressMaxLength);

    /// <summary>اسم مالك المصنع: اختياري وسقفه 200 (FactoryConfiguration.OwnerName).</summary>
    public static IRuleBuilderOptions<T, string?> FactoryOwnerName<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(200, Messages.FactoryOwnerNameMaxLength);

    // ============================================================
    // نوع الخرسانة
    // ============================================================

    /// <summary>اسم نوع الخرسانة: مطلوب وسقفه 100.</summary>
    public static IRuleBuilderOptions<T, string?> ConcreteTypeName<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(100, Messages.ConcreteTypeNameRequired, Messages.ConcreteTypeNameMaxLength);

    /// <summary>مقاومة نوع الخرسانة: أكبر من صفر.</summary>
    public static IRuleBuilderOptions<T, int> ConcreteTypeStrength<T>(this IRuleBuilder<T, int> builder)
        => builder.GreaterThan(0).WithMessage(Messages.ConcreteTypeStrengthInvalid);

    /// <summary>
    /// سعر متر نوع الخرسانة: أكبر من صفر.
    /// منفصل عن <see cref="Messages.UnitPriceMustBePositive"/> (سعر متر الطلب) لأن نصّيهما مختلفان.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> ConcreteTypeUnitPrice<T>(this IRuleBuilder<T, decimal> builder)
        => builder.GreaterThan(0).WithMessage(Messages.ConcreteTypePriceMustBePositive);

    // ============================================================
    // الطلب — النصوص الحرّة (السقوف = HasMaxLength في OrderConfiguration)
    // ============================================================

    /// <summary>اسم المشروع — سقفه 200.</summary>
    public static IRuleBuilderOptions<T, string?> ProjectName<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(200, Messages.ProjectNameMaxLength);

    /// <summary>اسم صاحب المشروع — سقفه 200.</summary>
    public static IRuleBuilderOptions<T, string?> ProjectOwnerName<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(200, Messages.ProjectOwnerNameMaxLength);

    /// <summary>منطقة الموقع — سقفها 100.</summary>
    public static IRuleBuilderOptions<T, string?> SiteArea<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(100, Messages.SiteAreaMaxLength);

    /// <summary>وصف الموقع — سقفه 500.</summary>
    public static IRuleBuilderOptions<T, string?> SiteDescription<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(500, Messages.SiteDescriptionMaxLength);

    /// <summary>الملاحظات — سقفها 1000.</summary>
    public static IRuleBuilderOptions<T, string?> Notes<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(1000, Messages.NotesMaxLength);

    // ============================================================
    // الطلب — المفاتيح والأرقام والحالات
    // ============================================================

    /// <summary>معرّف المصنع: يجب تحديده.</summary>
    public static IRuleBuilderOptions<T, int> FactoryId<T>(this IRuleBuilder<T, int> builder)
        => builder.GreaterThan(0).WithMessage(Messages.FactoryRequired);

    /// <summary>معرّف نوع الخرسانة: يجب تحديده.</summary>
    public static IRuleBuilderOptions<T, int> ConcreteTypeId<T>(this IRuleBuilder<T, int> builder)
        => builder.GreaterThan(0).WithMessage(Messages.ConcreteTypeRequired);

    /// <summary>الكمية: أكبر من صفر ولا تتجاوز 1000 م³.</summary>
    public static IRuleBuilderOptions<T, decimal> Quantity<T>(this IRuleBuilder<T, decimal> builder)
        => builder
            .GreaterThan(0).WithMessage(Messages.QuantityMustBePositive)
            .LessThanOrEqualTo(1000).WithMessage(Messages.QuantityTooLarge);

    /// <summary>طريقة النقل: قيمة معرَّفة في <see cref="TransportMethod"/>.</summary>
    public static IRuleBuilderOptions<T, TransportMethod> TransportMethod<T>(
        this IRuleBuilder<T, TransportMethod> builder)
        => builder.IsInEnum().WithMessage(Messages.TransportMethodInvalid);

    /// <summary>نوع الصبة: قيمة معرَّفة في <see cref="SlabType"/>.</summary>
    public static IRuleBuilderOptions<T, SlabType> SlabType<T>(this IRuleBuilder<T, SlabType> builder)
        => builder.IsInEnum().WithMessage(Messages.SlabTypeInvalid);

    /// <summary>تاريخ الصب: لا يكون في الماضي (ويُقبل الغياب).</summary>
    public static IRuleBuilderOptions<T, DateTime?> PouringDate<T>(this IRuleBuilder<T, DateTime?> builder)
        => builder
            .Must(date => date is null || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage(Messages.PouringDateCannotBeInPast);

    /// <summary>
    /// رقم الطابق عند المضخة: مطلوب عند <paramref name="needsPump"/> وغير سالب.
    /// <c>GreaterThanOrEqualTo</c> تتجاهل <c>null</c> بنفسها، فلا حاجة لشرط <c>HasValue</c>.
    /// </summary>
    public static IRuleBuilderOptions<T, int?> PumpFloorNumber<T>(
        this IRuleBuilder<T, int?> builder, Func<T, bool> needsPump)
        => builder
            .NotNull().When(needsPump).WithMessage(Messages.PumpRequiresFloorNumber)
            .GreaterThanOrEqualTo(0).When(needsPump).WithMessage(Messages.FloorNumberMustBeNonNegative);

    // ============================================================
    // المستخدم والسائق
    // ============================================================

    /// <summary>دور المستخدم: قيمة معرَّفة في <see cref="UserRole"/>.</summary>
    public static IRuleBuilderOptions<T, UserRole> Role<T>(this IRuleBuilder<T, UserRole> builder)
        => builder.IsInEnum().WithMessage(Messages.InvalidRole);

    /// <summary>معرّف السائق: يجب تحديده.</summary>
    public static IRuleBuilderOptions<T, int> DriverId<T>(this IRuleBuilder<T, int> builder)
        => builder.GreaterThan(0).WithMessage(Messages.DriverRequired);

    /// <summary>رقم لوحة الشاحنة: مطلوب وسقفه 30.</summary>
    public static IRuleBuilderOptions<T, string?> TruckPlate<T>(this IRuleBuilder<T, string?> builder)
        => builder.RequiredCappedAt(30, Messages.TruckPlateRequired, Messages.TruckPlateMaxLength);

    /// <summary>سبب الرفض: اختياري وسقفه 500.</summary>
    public static IRuleBuilderOptions<T, string?> RejectionReason<T>(this IRuleBuilder<T, string?> builder)
        => builder.CappedAt(500, Messages.RejectionReasonMaxLength);
}
