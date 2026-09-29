using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Auth;
using Kharasana.Application.Validators.ConcreteType;
using Kharasana.Application.Validators.Factory;
using Kharasana.Application.Validators.Order;
using Kharasana.Application.Validators.User;
using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حارس توحيد المُدقّقات — يمنع تكرار علّة «نفس القاعدة بمصدرين مختلفين».
///
/// <para><b>العلّة الأصلية:</b> قاعدة البريد الإلكتروني كانت مكتوبة ثلاث مرات في ثلاثة
/// مُدقّقات: واحدة بِـ <c>MaximumLength(256)</c> ورسائل من <c>Messages</c>، واثنتان بلا
/// سقف إطلاقاً وبنصّ عربي صريح. فبينما يعيد تعديل المستخدم 400 لبريد أطول من العمود،
/// كان التسجيل الذاتي وإنشاء المستخدم يمرّان إلى <c>nvarchar(256)</c> فيرمي SQL Server
/// الخطأ 8152 فيصير الرد 500 بدل 400. ومثله خمس قواعد في <c>PhoneOrderDtoValidator</c>
/// كانت نصوصاً صريحة لها ثوابت مطابقة تماماً في <c>Messages</c>.</para>
///
/// <para><b>الفحص سلوكي لا انعكاسي:</b> يبني مدخلات تتجاوز حدود العمود ويؤكّد أن كل
/// مُدقّق يرفضها بالرسالة المشتركة نفسها. إعادة أي نسخة محلية من القاعدة — أو إسقاط
/// سقف العمود من أي مسار — تُسقط هذه الاختبارات فوراً.</para>
/// </summary>
public class ValidationRulesUnificationTests
{
    /// <summary>قواعد الطلب المشتركة بين إنشاء الطلب والطلب الهاتفي.</summary>
    private static readonly string[] SharedOrderRuleMessages =
    [
        Messages.FactoryRequired,
        Messages.ConcreteTypeRequired,
        Messages.QuantityMustBePositive,
        Messages.TransportMethodInvalid,
        Messages.SlabTypeInvalid
    ];

    /// <summary>بريد صالح الصيغة لكنه يتجاوز سقف عمود <c>Users.Email</c> (256).</summary>
    private static string EmailLongerThanColumn() => new string('a', 250) + "@example.com";

    /// <summary>كلمة مرور مقبولة الطول — من المصدر الواحد لا من رقم مكتوب في الاختبار.</summary>
    private static string ValidPassword() => new string('a', PasswordPolicy.MinimumLength);

    [Fact]
    public void EmailLongerThanTheColumn_IsRejectedByAllThreeEmailValidators()
    {
        var email = EmailLongerThanColumn();
        email.Length.Should().BeGreaterThan(256,
            "الاختبار يفقد معناه إن لم يتجاوز النصُّ سقفَ العمود فعلاً");

        var createUser = new CreateUserDtoValidator()
            .Validate(new CreateUserDto
            {
                FullName = "مستخدم تجريبي",
                Email = email,
                Password = ValidPassword(),
                Role = UserRole.Client
            })
            .Errors.Select(e => e.ErrorMessage);

        var updateUser = new UpdateUserDtoValidator()
            .Validate(new UpdateUserDto
            {
                FullName = "مستخدم تجريبي",
                Email = email,
                Role = UserRole.Client
            })
            .Errors.Select(e => e.ErrorMessage);

        var register = new RegisterClientValidator()
            .Validate(new RegisterUserDto
            {
                FullName = "عميل تجريبي",
                Email = email,
                Password = ValidPassword(),
                ConfirmPassword = ValidPassword()
            })
            .Errors.Select(e => e.ErrorMessage);

        createUser.Should().Contain(Messages.EmailMaxLength,
            "إنشاء المستخدم كان بلا سقف للبريد فيمرّ النصّ إلى العمود ويصير الرد 500");
        updateUser.Should().Contain(Messages.EmailMaxLength);
        register.Should().Contain(Messages.EmailMaxLength,
            "التسجيل الذاتي كان بلا سقف للبريد كذلك");
    }

    [Fact]
    public void OrderAndPhoneOrderValidators_EmitTheSharedMessageForEveryCommonRule()
    {
        var create = new CreateOrderDtoValidator()
            .Validate(new CreateOrderDto
            {
                FactoryId = 0,
                ConcreteTypeId = 0,
                Quantity = 0m,
                TransportMethod = (TransportMethod)999,
                SlabType = (SlabType)999
            })
            .Errors.Select(e => e.ErrorMessage)
            .ToList();

        var phone = new PhoneOrderDtoValidator()
            .Validate(new PhoneOrderDto
            {
                FactoryId = 0,
                ConcreteTypeId = 0,
                Quantity = 0m,
                TransportMethod = (TransportMethod)999,
                SlabType = (SlabType)999
            })
            .Errors.Select(e => e.ErrorMessage)
            .ToList();

        foreach (var message in SharedOrderRuleMessages)
        {
            create.Should().Contain(message,
                $"CreateOrderDtoValidator يجب أن يستعمل الرسالة المشتركة لا نسخة محلية منها");
            phone.Should().Contain(message,
                $"PhoneOrderDtoValidator يجب أن يستعمل الرسالة المشتركة لا نسخة محلية منها");
        }
    }

    [Fact]
    public void ConfirmPasswordRequired_ComesFromTheSharedConstantInBothValidatorAndDto()
    {
        var fromValidator = new RegisterClientValidator()
            .Validate(new RegisterUserDto
            {
                FullName = "عميل تجريبي",
                Password = ValidPassword(),
                ConfirmPassword = string.Empty
            })
            .Errors.Select(e => e.ErrorMessage);

        fromValidator.Should().Contain(Messages.ConfirmPasswordRequired);

        var fromAnnotation = typeof(RegisterUserDto)
            .GetProperty(nameof(RegisterUserDto.ConfirmPassword))!
            .GetCustomAttribute<RequiredAttribute>()!
            .ErrorMessage;

        fromAnnotation.Should().Be(Messages.ConfirmPasswordRequired,
            "رسالة السمة والنصّ الصريح في المُدقّق يجب أن يكونا مصدراً واحداً");
    }

    /// <summary>
    /// كل سقف في مُدقّق المصنع يجب أن يحمل رسالته العربية.
    ///
    /// <para>العلّة التي يمنعها: كان <c>WithMessage</c> يُكتب <b>قبل</b> <c>MaximumLength</c>
    /// فيسري على <c>NotEmpty</c> وحده، ويسقط فرع السقف إلى رسالة FluentValidation
    /// الإنجليزية («'Owner Name' must be 200 characters or less.») في نظام عربي بالكامل.
    /// هذا الاختبار يقارن المجموعة كاملة، فيسقط عند أي سقوط إلى الافتراضية.</para>
    /// </summary>
    [Fact]
    public void FactoryValidator_ReportsEveryCappedFieldInArabic()
    {
        var expected = new[]
        {
            Messages.FactoryNameMaxLength,
            Messages.FactoryAreaMaxLength,
            Messages.FactoryAddressMaxLength,
            Messages.FactoryOwnerNameMaxLength,
            Messages.EmailMaxLength
        };

        var fromCreate = new CreateFactoryDtoValidator()
            .Validate(new CreateFactoryDto
            {
                FactoryName = new string('x', 201),
                Area = new string('x', 101),
                Address = new string('x', 301),
                OwnerName = new string('x', 201),
                Email = EmailLongerThanColumn()
            })
            .Errors.Select(e => e.ErrorMessage);

        fromCreate.Should().BeEquivalentTo(expected,
            "لا يجوز أن تسقط أي رسالة سقف إلى رسالة FluentValidation الإنجليزية الافتراضية");
    }

    /// <summary>مُدقّقا المصنع (إنشاء/تعديل) كانا نسخة واحدة مكرّرة حرفياً — فيبقيان متطابقين.</summary>
    [Fact]
    public void FactoryCreateAndUpdateValidators_EmitTheSameMessages()
    {
        var fromCreate = new CreateFactoryDtoValidator()
            .Validate(new CreateFactoryDto
            {
                FactoryName = new string('x', 201),
                Area = new string('x', 101),
                Address = new string('x', 301),
                OwnerName = new string('x', 201),
                Email = EmailLongerThanColumn()
            })
            .Errors.Select(e => e.ErrorMessage).ToList();

        var fromUpdate = new UpdateFactoryDtoValidator()
            .Validate(new UpdateFactoryDto
            {
                FactoryName = new string('x', 201),
                Area = new string('x', 101),
                Address = new string('x', 301),
                OwnerName = new string('x', 201),
                Email = EmailLongerThanColumn()
            })
            .Errors.Select(e => e.ErrorMessage).ToList();

        fromUpdate.Should().BeEquivalentTo(fromCreate,
            "أي انحراف بينهما يعني عودة نسختين من القاعدة الواحدة");
    }

    /// <summary>مُدقّقا نوع الخرسانة — الفرق الوحيد المقصود هو FactoryId في مسار الإنشاء.</summary>
    [Fact]
    public void ConcreteTypeValidators_DifferOnlyByTheFactoryIdRule()
    {
        var shared = new[]
        {
            Messages.ConcreteTypeNameRequired,
            Messages.ConcreteTypeStrengthInvalid,
            Messages.ConcreteTypePriceMustBePositive
        };

        var fromCreate = new CreateConcreteTypeDtoValidator()
            .Validate(new CreateConcreteTypeDto
            {
                FactoryId = 0,
                Name = string.Empty,
                Strength = 0,
                UnitPrice = 0m
            })
            .Errors.Select(e => e.ErrorMessage).ToList();

        var fromUpdate = new UpdateConcreteTypeDtoValidator()
            .Validate(new UpdateConcreteTypeDto
            {
                Name = string.Empty,
                Strength = 0,
                UnitPrice = 0m
            })
            .Errors.Select(e => e.ErrorMessage).ToList();

        fromCreate.Should().BeEquivalentTo(shared.Append(Messages.FactoryRequired));
        fromUpdate.Should().BeEquivalentTo(shared);
    }
}
