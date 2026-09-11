using System.ComponentModel.DataAnnotations;

namespace Kharasana.Domain.Validation;

/// <summary>
/// سمة تحقق مركزيّة من أرقام الهواتف المحمولة اليمنية.
///
/// البادئات المدعومة: 70, 71, 73, 77, 78
/// الصيغ المقبولة: 771234567, 0771234567, +967771234567, 967771234567, 00967771234567
///
/// القواعد:
/// - القيمة null أو فارغة ("" أو مسافات) تُعتبر <b>صالحة</b> لأن الحقل اختياري.
///   (تجنّباً لخطأ DataType attributes الشهير الذي يرفض السلاسل الفارغة)
/// - عند تقديم قيمة، يجب أن تتحوّل إلى رقم محمول يمني صحيح بعد التطبيع.
/// - لا تُعدّل السمة القيمة المخزنة — التطبيع إلى 967XXXXXXXXX يتم عبر
///   <see cref="YemeniPhone.Normalize"/> عند الحاجة (مثال: قبل الإرسال أو الحفظ).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class YemeniPhoneAttribute : ValidationAttribute
{
    // الأرقام الأساسية الثمانية المكوّنة للبادئات اليمنية (من 0 إلى 9)
    private const string Digits = "0123456789";

    // كل البادئات المدعومة: 70, 71, 73, 77, 78
    private static readonly string[] SupportedPrefixes = { "70", "71", "73", "77", "78" };

    /// <summary>
    /// يحوّل الأرقام العربية (٠١٢٣٤٥٦٧٨٩) والهندية (۰۱۲۳۴۵۶۷۸۹) إلى أرقام إنجليزية.
    /// </summary>
    public static string ConvertArabicIndicDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        Span<char> buffer = stackalloc char[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            buffer[i] = c switch
            {
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')), // عربية
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')), // هندية
                _ => c
            };
        }
        return new string(buffer);
    }

    /// <summary>
    /// يزيل أي أحرف غير رقمية (مسافات، شرطات، أقواس، علامة + ... إلخ).
    /// </summary>
    public static string StripNonDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (char c in input)
            if (c is >= '0' and <= '9')
                sb.Append(c);
        return sb.ToString();
    }

    /// <summary>
    /// يوحّد أي صيغة هاتف مقبولة إلى الصيغة الدولية الكنسية 967XXXXXXXXX.
    /// يعيد القيمة المطبَّعة، أو null إذا كانت القيمة غير صالحة (فارغة أو غير يمنية).
    ///
    /// الخوارزمية:
    /// 1) تحويل الأرقام العربية/الهندية إلى أرقام إنجليزية.
    /// 2) إزالة كل الأحرف غير الرقمية (مسافات، شرطات، أقواس، + ...).
    /// 3) إزالة مُعرّفات الدولة والصفر المحلي: 00 ثم 967 أو 0.
    /// 4) يجب أن تبقى 9 أرقام محلية تبدأ ببادئة مدعومة (70/71/73/77/78).
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string digits = StripNonDigits(ConvertArabicIndicDigits(value));

        if (digits.Length == 0)
            return null;

        // إزالة 00 قبل رمز الدولة: 00967771234567 → 967771234567
        if (digits.StartsWith("00967", StringComparison.Ordinal))
            digits = digits[2..];

        // إما رمز دولة 967، أو صفر محلي واحد (أو كلاهما إذا أُدخل مثل 9670771234567)
        if (digits.StartsWith("967", StringComparison.Ordinal))
            digits = digits[3..];
        else if (digits.StartsWith('0'))
            digits = digits[1..];

        if (digits.StartsWith('0'))
            digits = digits[1..];

        if (digits.Length != 9)
            return null;

        if (!IsValidYemeniMobile(digits))
            return null;

        return "967" + digits;
    }

    private static bool IsValidYemeniMobile(string national)
    {
        if (national.Length != 9)
            return false;

        foreach (string prefix in SupportedPrefixes)
            if (national.StartsWith(prefix, StringComparison.Ordinal))
                return true;

        return false;
    }

    public override bool IsValid(object? value)
    {
        // null أو سلسلة فارغة/مسافات = صالحة (الحقل اختياري)
        if (value is null)
            return true;

        if (value is not string str)
            return false;

        if (string.IsNullOrWhiteSpace(str))
            return true;

        return Normalize(str) != null;
    }
}
