using System.Linq;

namespace Kharasana.Domain.Common;

/// <summary>
/// توحيد أرقام الهواتف اليمنية إلى صيغة قياسية: 967XXXXXXXXX (بدون + وبدون فراغات)
/// يقبل: +967771234567 / 00967771234567 / 0771234567 / 771234567 / ٠٧٧١٢٣٤٥٦٧
/// </summary>
public static class YemeniPhoneHelper
{
    public static string? Normalize(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return null;

        var digitsOnly = new string(ToLatinDigits(rawPhone).Where(char.IsDigit).ToArray());

        if (digitsOnly.StartsWith("00967"))
            digitsOnly = digitsOnly.Substring(5);
        else if (digitsOnly.StartsWith("967"))
            digitsOnly = digitsOnly.Substring(3);
        else if (digitsOnly.StartsWith("0"))
            digitsOnly = digitsOnly.Substring(1);

        if (digitsOnly.Length != 9 || !digitsOnly.StartsWith("7"))
            return null;

        return "967" + digitsOnly;
    }

    /// <summary>
    /// تحويل الأرقام العربية الهندية (٠-٩) والفارسية (۰-۹) إلى أرقام لاتينية.
    /// شائعة في إدخال أرقام الهواتف اليمنية، لذا تُعالج قبل استخراج الأرقام.
    /// </summary>
    private static string ToLatinDigits(string input)
    {
        var chars = new char[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            chars[i] = input[i] switch
            {
                '٠' => '0', '١' => '1', '٢' => '2', '٣' => '3', '٤' => '4',
                '٥' => '5', '٦' => '6', '٧' => '7', '٨' => '8', '٩' => '9',
                '۰' => '0', '۱' => '1', '۲' => '2', '۳' => '3', '۴' => '4',
                '۵' => '5', '۶' => '6', '۷' => '7', '۸' => '8', '۹' => '9',
                _ => input[i]
            };
        }
        return new string(chars);
    }

    public static bool IsValid(string? rawPhone) => Normalize(rawPhone) != null;

    /// <summary>
    /// تطبيع مرن لغرض البحث الجزئي فقط (لا يشترط رقمًا كاملًا صالحًا).
    /// يزيل صفر البداية / رمز الدولة إن وُجد، ليتطابق مع الصيغة المخزّنة (967XXXXXXXXX).
    /// مثال: "0771234567" → "771234567"، "+967771" → "967771" يبقى كما هو (قصير).
    /// </summary>
    public static string NormalizeForSearch(string? rawTerm)
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
            return string.Empty;

        var digitsOnly = new string(ToLatinDigits(rawTerm).Where(char.IsDigit).ToArray());

        if (digitsOnly.StartsWith("00967"))
            digitsOnly = digitsOnly.Substring(5);
        else if (digitsOnly.StartsWith("967") && digitsOnly.Length > 9)
            digitsOnly = digitsOnly.Substring(3);
        else if (digitsOnly.StartsWith("0") && digitsOnly.Length > 1)
            digitsOnly = digitsOnly.Substring(1);

        return digitsOnly;
    }
}