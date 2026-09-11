using System.Linq;

namespace Kharasana.Application.Common;

/// <summary>
/// توحيد أرقام الهواتف اليمنية إلى صيغة قياسية: 967XXXXXXXXX (بدون + وبدون فراغات)
/// يقبل: +967771234567 / 00967771234567 / 0771234567 / 771234567
/// </summary>
public static class YemeniPhoneHelper
{
    public static string? Normalize(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return null;

        var digitsOnly = new string(rawPhone.Where(char.IsDigit).ToArray());

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

    public static bool IsValid(string? rawPhone) => Normalize(rawPhone) != null;

    /// <summary>
    /// تطبيع مرن لغرض البحث الجزئي فقط (لا يشترط رقمًا كاملاً صالحًا).
    /// يزيل صفر البداية / رمز الدولة إن وُجد، ليتطابق مع الصيغة المخزّنة (967XXXXXXXXX).
    /// مثال: "0771234567" → "771234567"، "+967771" → "967771" يبقى كما هو (قصير).
    /// </summary>
    public static string NormalizeForSearch(string? rawTerm)
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
            return string.Empty;

        var digitsOnly = new string(rawTerm.Where(char.IsDigit).ToArray());

        if (digitsOnly.StartsWith("00967"))
            digitsOnly = digitsOnly.Substring(5);
        else if (digitsOnly.StartsWith("967") && digitsOnly.Length > 9)
            digitsOnly = digitsOnly.Substring(3);
        else if (digitsOnly.StartsWith("0") && digitsOnly.Length > 1)
            digitsOnly = digitsOnly.Substring(1);

        return digitsOnly;
    }
}