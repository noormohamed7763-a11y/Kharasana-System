namespace Kharasana.Application.Common;

/// <summary>
/// توحيد الاختلافات الشائعة في كتابة الأسماء العربية عند البحث،
/// دون تعديل البيانات الأصلية المخزّنة (تُطبَّق فقط أثناء المقارنة).
/// </summary>
public static class ArabicTextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Replace("أ", "ا")
            .Replace("إ", "ا")
            .Replace("آ", "ا")
            .Replace("ى", "ي")
            .Replace("ة", "ه");
    }
}