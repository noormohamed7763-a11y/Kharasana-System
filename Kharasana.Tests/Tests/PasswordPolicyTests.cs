using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FluentAssertions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Auth;
using Kharasana.Application.DTOs.User;
using Kharasana.Application.Validators.Auth;
using Kharasana.Application.Validators.User;
using Kharasana.Domain.Enums;
using Kharasana.Domain.Validation;
using Xunit;

namespace Kharasana.Tests.Tests;

/// <summary>
/// حراسة توحيد سياسة كلمة المرور.
///
/// <para>قبل التوحيد كان الحد الأدنى مكتوباً بالرقم في ستة مواضع بقيم متعارضة:
/// 6 في <c>RegisterUserDto</c> وثلاثة ViewModels في الويب ووسوم <c>minlength</c>
/// وفحص JavaScript، و8 في <c>ValidationRules.Password</c> — بينما رسالة الخطأ
/// <c>Messages.PasswordMinLength</c> تقول «8 أحرف» دائماً. النتيجة: التسجيل العام
/// يقبل كلمة مرور من 6 أحرف، ورسالة الخطأ تكذب، وإنشاء المستخدمين يقبل محرفاً واحداً.</para>
///
/// <para>الآن الرقم في <see cref="PasswordPolicy.MinimumLength"/> وحده، وهذه الاختبارات
/// تُسقط البناء إن انفصل عنه أي موضع أو انفصلت عنه الرسالة.</para>
/// </summary>
public class PasswordPolicyTests
{
    [Fact]
    public void PasswordPolicy_IsTheExpectedMinimum()
    {
        // تغيير هذا الرقم قرار منتج: يجب أن يرافقه تحديث Messages.PasswordMinLength
        // ووسوم minlength في الواجهات — والاختبارات أدناه تدلّك على المواضع.
        PasswordPolicy.MinimumLength.Should().Be(8);
    }

    [Fact]
    public void PasswordMinLengthMessage_MatchesThePolicy()
    {
        Messages.PasswordMinLength.Should().Contain(
            PasswordPolicy.MinimumLength.ToString(),
            "رسالة الخطأ تعرض الرقم للمستخدم — رسالة تقول «6» وسياسة تفرض 8 تُضلّل المستخدم");
    }

    [Fact]
    public void RegisterDto_MinLengthAttribute_MatchesThePolicy()
    {
        var attribute = typeof(RegisterUserDto)
            .GetProperty(nameof(RegisterUserDto.Password))!
            .GetCustomAttribute<MinLengthAttribute>();

        attribute.Should().NotBeNull("الحارس الأول للتسجيل العام هو DataAnnotations قبل أي فلتر");
        attribute!.Length.Should().Be(PasswordPolicy.MinimumLength);
    }

    [Fact]
    public async Task RegisterValidator_EnforcesThePolicy()
    {
        // الحارس الثاني بعد DataAnnotations (يُشغَّل عبر ValidationFilter على AuthController.Register)
        var validator = new RegisterClientValidator();
        var shortPassword = new string('a', PasswordPolicy.MinimumLength - 1);

        var result = await validator.ValidateAsync(new RegisterUserDto
        {
            FullName = "عميل",
            Phone = "771234567",
            Password = shortPassword,
            ConfirmPassword = shortPassword
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.PasswordMinLength);
    }

    /// <summary>
    /// القاعدة المشتركة <c>ValidationRules.Password()</c> تفرض الحد الأدنى بالضبط:
    /// محرف أقل يُرفض، والحد الأدنى نفسه يُقبل. هذا ما يثبت أن الرقم مأخوذ من
    /// الثابت لا مكتوباً بقيمة أخرى داخل القاعدة.
    /// </summary>
    [Fact]
    public async Task SharedPasswordRule_EnforcesExactlyThePolicyBoundary()
    {
        var validator = new CreateUserDtoValidator();

        CreateUserDto UserWithPassword(string password) => new()
        {
            FullName = "سائق",
            Phone = "771234567",
            Password = password,
            Role = UserRole.Driver,
            FactoryId = 1
        };

        var belowMinimum = await validator.ValidateAsync(
            UserWithPassword(new string('a', PasswordPolicy.MinimumLength - 1)));

        belowMinimum.IsValid.Should().BeFalse();
        belowMinimum.Errors.Select(e => e.ErrorMessage).Should().Contain(Messages.PasswordMinLength);

        var atMinimum = await validator.ValidateAsync(
            UserWithPassword(new string('a', PasswordPolicy.MinimumLength)));

        atMinimum.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UserRole_NumericValues_AreTheContractWithWebAndJavaScript()
    {
        // ✅ حارس علّة «حساب المصنع»: كانت factories.js ترسل role: 1 وقيمة الحقل المخفي 1
        //    حين كان FactoryEmployee = 1. بعد إعادة الترقيم (Admin=1..Client=4) صار
        //    1 = Admin، فكان طلب «إنشاء حساب مصنع» يُرفض دائماً ولا أحد يلاحظ.
        //    أي تغيير في هذه الأرقام يجب أن يرافقه فحص للواجهات التي تُرسل الأدوار.
        ((int)UserRole.Admin).Should().Be(1);
        ((int)UserRole.FactoryEmployee).Should().Be(2);
        ((int)UserRole.Driver).Should().Be(3);
        ((int)UserRole.Client).Should().Be(4);
    }
}
