using FluentValidation;
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
/// اختبارات قواعد المُدقّقات العشرة التي كانت معطّلة (لا ValidationFilter على نقاط نهايتها).
/// كل اختبار هنا يقابل ثغرة أو قاعدة كانت مكتوبة في الكود ولا تُنفَّذ على أي طلب.
/// </summary>
public class ValidatorRuleTests
{
    private static async Task AssertInvalidAsync<T>(IValidator<T> validator, T model, string because)
    {
        var result = await validator.ValidateAsync(model);
        result.IsValid.Should().BeFalse(because);
    }

    private static async Task AssertValidAsync<T>(IValidator<T> validator, T model, string because)
    {
        var result = await validator.ValidateAsync(model);
        result.Errors.Should().BeEmpty($"{because} — الأخطاء: " +
            string.Join(" | ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
    }

    // ═══════════════════════════════════════════════════════════
    // ⑪ Auth — LoginRequestDtoValidator
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null, "Test@1234")]      // بلا مُعرّف
    [InlineData("", "Test@1234")]        // مُعرّف فارغ
    [InlineData("771234567", null)]      // بلا كلمة مرور
    [InlineData("771234567", "")]        // كلمة مرور فارغة
    public async Task Login_MissingCredentials_IsInvalid(string? emailOrPhone, string? password)
    {
        var validator = new LoginRequestDtoValidator();
        var dto = new LoginRequestDto { EmailOrPhone = emailOrPhone!, Password = password! };

        await AssertInvalidAsync(validator, dto, "الدخول يتطلّب مُعرّفاً وكلمة مرور");
    }

    [Fact]
    public async Task Login_BothProvided_IsValid()
    {
        var validator = new LoginRequestDtoValidator();
        var dto = new LoginRequestDto { EmailOrPhone = "771234567", Password = "Test@1234" };

        await AssertValidAsync(validator, dto, "بيانات دخول مكتملة");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑫ Auth — RegisterClientValidator (التسجيل العام)
    // ═══════════════════════════════════════════════════════════

    private static RegisterUserDto ValidRegistration(string password = "Test@1234") => new()
    {
        FullName = "عميل جديد",
        Phone = "771234567",
        Password = password,
        ConfirmPassword = password
    };

    [Fact]
    public async Task Register_WithoutEmail_IsValid()
    {
        // ✅ حارس انحدار: كان المُدقّق يشترط Email (NotEmpty) بينما RegisterUserDto
        //    جعل البريد اختيارياً و [YemeniEmail] يعتبر الفارغ صالحاً. توصيل المُدقّق
        //    كما كان سيُفشل تسجيل كل عميل بلا بريد — وهو المسار الأساسي في اليمن.
        var validator = new RegisterClientValidator();
        var dto = ValidRegistration();
        dto.Email = null;

        await AssertValidAsync(validator, dto, "الهاتف وحده كافٍ للتسجيل الذاتي");
    }

    [Theory]
    [InlineData("1234567")]     // 7 أحرف — كان مقبولاً (MinLength(6)) قبل التوحيد
    [InlineData("12345")]
    [InlineData("")]
    public async Task Register_PasswordBelowMinimum_IsInvalid(string password)
    {
        var validator = new RegisterClientValidator();
        var dto = ValidRegistration(password);

        await AssertInvalidAsync(validator, dto,
            $"الحد الأدنى الموحّد {PasswordPolicy.MinimumLength} أحرف");
    }

    [Fact]
    public async Task Register_PasswordExactlyAtMinimum_IsValid()
    {
        var validator = new RegisterClientValidator();
        var dto = ValidRegistration(new string('a', PasswordPolicy.MinimumLength));

        await AssertValidAsync(validator, dto, $"الطول {PasswordPolicy.MinimumLength} مطابق للحد الأدنى");
    }

    [Fact]
    public async Task Register_PasswordMismatch_IsInvalid()
    {
        var validator = new RegisterClientValidator();
        var dto = ValidRegistration();
        dto.ConfirmPassword = "Different@9999";

        await AssertInvalidAsync(validator, dto, "تأكيد كلمة المرور يجب أن يطابقها");
    }

    [Fact]
    public async Task Register_InvalidEmailOrPhone_IsInvalid()
    {
        var validator = new RegisterClientValidator();

        var badEmail = ValidRegistration();
        badEmail.Email = "ليس-بريداً";
        await AssertInvalidAsync(validator, badEmail, "صيغة البريد غير صالحة");

        var badPhone = ValidRegistration();
        badPhone.Phone = "123";
        await AssertInvalidAsync(validator, badPhone, "رقم الهاتف يجب أن يكون يمنياً صالحاً");

        var badWhatsApp = ValidRegistration();
        badWhatsApp.WhatsApp = "abc";
        await AssertInvalidAsync(validator, badWhatsApp, "رقم الواتساب يجب أن يكون يمنياً صالحاً");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑬ User — CreateUserDtoValidator (الثغرة المُثبَتة)
    // ═══════════════════════════════════════════════════════════

    private static CreateUserDto ValidNewUser() => new()
    {
        FullName = "سائق جديد",
        Phone = "771234567",
        Password = "Test@1234",
        Role = UserRole.Driver,
        FactoryId = 1
    };

    [Theory]
    [InlineData("a")]         // ✅ الثغرة: POST /api/Users بكلمة مرور من محرف واحد
    [InlineData("1234567")]
    [InlineData("")]
    public async Task CreateUser_ShortPassword_IsInvalid(string password)
    {
        var validator = new CreateUserDtoValidator();
        var dto = ValidNewUser();
        dto.Password = password;

        await AssertInvalidAsync(validator, dto, "كلمة المرور تحت الحد الأدنى الموحّد");
    }

    [Fact]
    public async Task CreateUser_AdminRole_IsInvalid()
    {
        // المُدقّق يردّ 400، والخدمة تُعيد الفحص بنفسها (ForbiddenException) — دفاع في العمق
        var validator = new CreateUserDtoValidator();
        var dto = ValidNewUser();
        dto.Role = UserRole.Admin;

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.CannotCreateAdmin);
    }

    [Fact]
    public async Task CreateUser_ValidDriver_IsValid()
    {
        var validator = new CreateUserDtoValidator();

        await AssertValidAsync(validator, ValidNewUser(), "سائق بمصنع وكلمة مرور مطابقة");
    }

    [Fact]
    public async Task CreateUser_EmailIsOptionalButMustBeWellFormed()
    {
        var validator = new CreateUserDtoValidator();

        var withoutEmail = ValidNewUser();
        withoutEmail.Email = null;
        await AssertValidAsync(validator, withoutEmail, "البريد اختياري (الهاتف بديل)");

        var badEmail = ValidNewUser();
        badEmail.Email = "ليس-بريداً";
        await AssertInvalidAsync(validator, badEmail, "البريد إن وُجد يجب أن يكون صالحاً");
    }

    [Fact]
    public async Task CreateUser_InvalidRoleNumber_IsInvalid()
    {
        var validator = new CreateUserDtoValidator();
        var dto = ValidNewUser();
        dto.Role = (UserRole)99;

        await AssertInvalidAsync(validator, dto, "الدور يجب أن يكون من قيم UserRole");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑭ User — UpdateUserDtoValidator
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateUser_AdminRole_IsInvalid()
    {
        var validator = new UpdateUserDtoValidator();
        var dto = new UpdateUserDto { FullName = "مستخدم", Role = UserRole.Admin };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.CannotChangeToAdmin);
    }

    [Fact]
    public async Task UpdateUser_EmptyNameOrBadPhone_IsInvalid()
    {
        var validator = new UpdateUserDtoValidator();

        await AssertInvalidAsync(validator,
            new UpdateUserDto { FullName = "", Role = UserRole.Driver },
            "الاسم الكامل مطلوب");

        await AssertInvalidAsync(validator,
            new UpdateUserDto { FullName = "اسم", Role = UserRole.Driver, Phone = "123" },
            "رقم الهاتف يجب أن يكون يمنياً صالحاً");
    }

    [Fact]
    public async Task UpdateUser_Valid_IsValid()
    {
        var validator = new UpdateUserDtoValidator();
        var dto = new UpdateUserDto
        {
            FullName = "سائق معدّل",
            Role = UserRole.Driver,
            Phone = "771234567",
            WhatsApp = "771234567"
        };

        await AssertValidAsync(validator, dto, "بيانات صالحة");
    }

    [Fact]
    public async Task UpdateUser_EmailOptionalButMustBeWellFormedAndWithinColumn()
    {
        // ✅ حارس انحدار: UpdateUserDto كان بلا حقل Email أصلاً، فيُهمَل ما يرسله
        //    الويب (تعديل العميل/السائق/المستخدم) صامتاً مع رسالة نجاح.
        //    القاعدة الجديدة: الغياب يعني «أبقِ الحالي»، والصيغة والسقف يُفحصان إن وُجد.
        var validator = new UpdateUserDtoValidator();

        var withoutEmail = new UpdateUserDto { FullName = "مستخدم", Role = UserRole.Client };
        await AssertValidAsync(validator, withoutEmail, "غياب البريد يعني «أبقِ الحالي» لا خطأ");

        var emptyEmail = new UpdateUserDto { FullName = "مستخدم", Role = UserRole.Client, Email = "" };
        await AssertValidAsync(validator, emptyEmail, "الفراغ يعني «أبقِ الحالي» أيضاً");

        var badEmail = new UpdateUserDto { FullName = "مستخدم", Role = UserRole.Client, Email = "ليس-بريداً" };
        var badResult = await validator.ValidateAsync(badEmail);
        badResult.IsValid.Should().BeFalse("البريد إن وُجد يجب أن يكون صالحاً");
        badResult.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.InvalidEmail);

        // 250 + "@test.local" = 261 حرفاً — يتجاوز عمود البريد (256) فيرمي SQL Server
        // الخطأ 8152 (اقتطاع) ويصير الرد 500 بدل 400
        var longEmail = new UpdateUserDto
        {
            FullName = "مستخدم",
            Role = UserRole.Client,
            Email = new string('a', 250) + "@test.local"
        };
        var longResult = await validator.ValidateAsync(longEmail);
        longResult.IsValid.Should().BeFalse("البريد الأطول من عموده (256) مرفوض");
        longResult.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.EmailMaxLength);
    }

    // ═══════════════════════════════════════════════════════════
    // ⑮ User — UpdateMyProfileDtoValidator
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateMyProfile_EmptyName_IsInvalid()
    {
        var validator = new UpdateMyProfileDtoValidator();

        await AssertInvalidAsync(validator,
            new UpdateMyProfileDto { FullName = "   " },
            "الاسم الكامل مطلوب حتى في الملف الشخصي");
    }

    [Fact]
    public async Task UpdateMyProfile_Valid_IsValid()
    {
        var validator = new UpdateMyProfileDtoValidator();
        var dto = new UpdateMyProfileDto
        {
            FullName = "اسم صالح",
            Phone = "771234567",
            WhatsApp = "771234567"
        };

        await AssertValidAsync(validator, dto, "ملف شخصي صالح");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑯ User — UpdateDriverStatusDtoValidator
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task UpdateDriverStatus_UndefinedValue_IsInvalid(int rawStatus)
    {
        var validator = new UpdateDriverStatusDtoValidator();
        var dto = new UpdateDriverStatusDto { DriverStatus = (DriverStatus)rawStatus };

        await AssertInvalidAsync(validator, dto, "حالة السائق يجب أن تكون من قيم DriverStatus");
    }

    [Theory]
    [InlineData(DriverStatus.Available)]
    [InlineData(DriverStatus.Busy)]
    [InlineData(DriverStatus.Offline)]
    public async Task UpdateDriverStatus_DefinedValue_IsValid(DriverStatus status)
    {
        var validator = new UpdateDriverStatusDtoValidator();
        var dto = new UpdateDriverStatusDto { DriverStatus = status };

        await AssertValidAsync(validator, dto, "حالة معرَّفة");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑰ ConcreteType — الإنشاء والتعديل
    // ═══════════════════════════════════════════════════════════

    private static CreateConcreteTypeDto ValidConcreteType() => new()
    {
        FactoryId = 1,
        Name = "خرسانة 300",
        Strength = 300,
        UnitPrice = 15000m
    };

    [Fact]
    public async Task CreateConcreteType_Valid_IsValid()
    {
        var validator = new CreateConcreteTypeDtoValidator();

        await AssertValidAsync(validator, ValidConcreteType(), "نوع خرسانة مكتمل");
    }

    [Theory]
    [InlineData(0, "خرسانة", 300, 15000)]      // بلا مصنع
    [InlineData(1, "", 300, 15000)]            // بلا اسم
    [InlineData(1, "خرسانة", 0, 15000)]        // مقاومة صفر
    [InlineData(1, "خرسانة", 300, 0)]          // سعر صفر
    public async Task CreateConcreteType_MissingRequiredField_IsInvalid(
        int factoryId, string name, int strength, decimal unitPrice)
    {
        var validator = new CreateConcreteTypeDtoValidator();
        var dto = new CreateConcreteTypeDto
        {
            FactoryId = factoryId,
            Name = name,
            Strength = strength,
            UnitPrice = unitPrice
        };

        await AssertInvalidAsync(validator, dto, "كل حقول نوع الخرسانة إلزامية وموجبة");
    }

    [Fact]
    public async Task UpdateConcreteType_NonPositivePriceOrStrength_IsInvalid()
    {
        var validator = new UpdateConcreteTypeDtoValidator();

        await AssertInvalidAsync(validator,
            new UpdateConcreteTypeDto { Name = "خرسانة", Strength = 300, UnitPrice = 0 },
            "السعر يجب أن يكون أكبر من صفر");

        await AssertInvalidAsync(validator,
            new UpdateConcreteTypeDto { Name = "خرسانة", Strength = 0, UnitPrice = 100 },
            "المقاومة يجب أن تكون أكبر من صفر");

        await AssertValidAsync(validator,
            new UpdateConcreteTypeDto { Name = "خرسانة", Strength = 300, UnitPrice = 100, IsActive = true },
            "بيانات صالحة");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑱ Factory — الإنشاء والتعديل
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFactory_Valid_IsValid()
    {
        var validator = new CreateFactoryDtoValidator();
        var dto = new CreateFactoryDto
        {
            FactoryName = "مصنع اختبار",
            Area = "صنعاء",
            Address = "شارع الستين",
            OwnerName = "المالك",
            Phone = "771234567",
            WhatsApp = "771234567"
        };

        await AssertValidAsync(validator, dto, "مصنع مكتمل");
    }

    [Theory]
    [InlineData("", "صنعاء", "شارع")]      // بلا اسم
    [InlineData("مصنع", "", "شارع")]        // بلا منطقة
    [InlineData("مصنع", "صنعاء", "")]      // بلا عنوان
    public async Task CreateFactory_MissingRequiredField_IsInvalid(string name, string area, string address)
    {
        var validator = new CreateFactoryDtoValidator();
        var dto = new CreateFactoryDto { FactoryName = name, Area = area, Address = address };

        await AssertInvalidAsync(validator, dto, "اسم المصنع والمنطقة والعنوان إلزامية");
    }

    [Fact]
    public async Task Factory_EmailOptionalButMustBeWellFormed()
    {
        var createValidator = new CreateFactoryDtoValidator();

        var badEmail = new CreateFactoryDto
        {
            FactoryName = "مصنع", Area = "صنعاء", Address = "شارع", Email = "ليس-بريداً"
        };
        await AssertInvalidAsync(createValidator, badEmail, "البريد إن وُجد يجب أن يكون صالحاً");

        var noEmail = new CreateFactoryDto
        {
            FactoryName = "مصنع", Area = "صنعاء", Address = "شارع", Email = null
        };
        await AssertValidAsync(createValidator, noEmail, "البريد اختياري");

        var updateValidator = new UpdateFactoryDtoValidator();
        var badUpdate = new UpdateFactoryDto
        {
            FactoryName = "مصنع", Area = "صنعاء", Address = "شارع", Email = "ليس-بريداً"
        };
        await AssertInvalidAsync(updateValidator, badUpdate, "نفس القاعدة على التعديل");
    }

    [Fact]
    public async Task UpdateFactory_Valid_IsValid()
    {
        var validator = new UpdateFactoryDtoValidator();
        var dto = new UpdateFactoryDto
        {
            FactoryName = "مصنع معدّل",
            Area = "عدن",
            Address = "شارع الميناء",
            Phone = "771234567"
        };

        await AssertValidAsync(validator, dto, "بيانات صالحة");
    }

    // ═══════════════════════════════════════════════════════════
    // ⑲ Order — سقوف النصوص الحرة (كانت غائبة: 8152 → 500 بدل 400)
    // ═══════════════════════════════════════════════════════════

    private static PhoneOrderDto ValidPhoneOrder() => new()
    {
        ClientPhone = "771234567",
        ClientFullName = "عميل هاتفي",
        FactoryId = 1,
        ConcreteTypeId = 1,
        Quantity = 10,
        SlabType = SlabType.Roof,
        TransportMethod = TransportMethod.FactoryTransport
    };

    private static CreateOrderDto ValidCreateOrder() => new()
    {
        ClientId = 1,
        FactoryId = 1,
        ConcreteTypeId = 1,
        Quantity = 10,
        SlabType = SlabType.Roof,
        TransportMethod = TransportMethod.FactoryTransport
    };

    private static UpdateOrderDto ValidUpdateOrder() => new()
    {
        ConcreteTypeId = 1,
        Quantity = 10,
        SlabType = SlabType.Roof,
        TransportMethod = TransportMethod.FactoryTransport
    };

    [Fact]
    public async Task OrderText_ExactlyAtColumnLength_IsValid()
    {
        // الحدّ نفسه مقبول — الخطأ يبدأ عند 201/101/501/1001 لا عند 200/100/500/1000
        var phone = ValidPhoneOrder();
        phone.ClientFullName = new string('م', 200);   // User.FullName = 200
        phone.ProjectName = new string('م', 200);      // Order.ProjectName = 200
        phone.ProjectOwnerName = new string('م', 200); // Order.ProjectOwnerName = 200
        phone.SiteArea = new string('م', 100);         // Order.SiteArea = 100
        phone.SiteDescription = new string('م', 500);  // Order.SiteDescription = 500
        phone.Notes = new string('م', 1000);           // Order.Notes = 1000
        await AssertValidAsync(new PhoneOrderDtoValidator(), phone, "الحد الأقصى بالضبط مقبول");

        var create = ValidCreateOrder();
        create.ProjectName = new string('م', 200);
        create.Notes = new string('م', 1000);
        await AssertValidAsync(new CreateOrderDtoValidator(), create, "الحد الأقصى بالضبط مقبول");

        var update = ValidUpdateOrder();
        update.SiteArea = new string('م', 100);
        update.SiteDescription = new string('م', 500);
        await AssertValidAsync(new UpdateOrderDtoValidator(), update, "الحد الأقصى بالضبط مقبول");
    }

    [Fact]
    public async Task PhoneOrder_TextBeyondColumnLength_IsInvalid()
    {
        // ✅ حارس انحدار: بلا هذه السقوف يصل النص إلى SQL Server فيرمي الخطأ 8152
        //    (اقتطاع) → 500 بدل 400، ويُفقد الطلب كاملاً بسبب حرف زائد في الملاحظات.
        var validator = new PhoneOrderDtoValidator();

        var longClientName = ValidPhoneOrder();
        longClientName.ClientFullName = new string('م', 201);
        await AssertInvalidAsync(validator, longClientName, "اسم العميل أطول من عمود 200");

        var longProject = ValidPhoneOrder();
        longProject.ProjectName = new string('م', 201);
        await AssertInvalidAsync(validator, longProject, "اسم المشروع أطول من عمود 200");

        var longOwner = ValidPhoneOrder();
        longOwner.ProjectOwnerName = new string('م', 201);
        await AssertInvalidAsync(validator, longOwner, "اسم صاحب المشروع أطول من عمود 200");

        var longArea = ValidPhoneOrder();
        longArea.SiteArea = new string('م', 101);
        await AssertInvalidAsync(validator, longArea, "المنطقة أطول من عمود 100");

        var longDescription = ValidPhoneOrder();
        longDescription.SiteDescription = new string('م', 501);
        await AssertInvalidAsync(validator, longDescription, "وصف الموقع أطول من عمود 500");

        var longNotes = ValidPhoneOrder();
        longNotes.Notes = new string('م', 1001);
        await AssertInvalidAsync(validator, longNotes, "الملاحظات أطول من عمود 1000");
    }

    [Fact]
    public async Task CreateAndUpdateOrder_TextBeyondColumnLength_IsInvalid()
    {
        // نفس السقوف على مساري الإنشاء والتعديل. مسار التعديل هو الأوسع تعرّضاً:
        // EditOrderViewModel كان بلا [StringLength] أيضاً، فلا شيء يوقف النص قبله.
        var create = ValidCreateOrder();
        create.Notes = new string('م', 1001);
        await AssertInvalidAsync(new CreateOrderDtoValidator(), create, "الملاحظات أطول من عمود 1000");

        var update = ValidUpdateOrder();
        update.SiteDescription = new string('م', 501);
        await AssertInvalidAsync(new UpdateOrderDtoValidator(), update, "وصف الموقع أطول من عمود 500");

        var updateNotes = ValidUpdateOrder();
        updateNotes.Notes = new string('م', 1001);
        var result = await new UpdateOrderDtoValidator().ValidateAsync(updateNotes);

        result.IsValid.Should().BeFalse("الملاحظات أطول من عمود 1000");
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(
            Messages.NotesMaxLength,
            "الرسالة عربية من Messages وليست نصّ FluentValidation الإنجليزي الافتراضي");
    }
}
