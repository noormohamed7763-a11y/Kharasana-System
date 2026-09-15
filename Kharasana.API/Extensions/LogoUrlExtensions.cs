using Kharasana.Application.DTOs.Factory;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Extensions;

/// <summary>
/// تحويل مسار الشعار النسبي إلى رابط مطلق (scheme + host) في طبقة العرض.
/// المسار المخزّن في قاعدة البيانات نسبي (مثل /Images/Factories/x.png)،
/// بينما تُعطى الواجهة الخارجية رابطاً كاملاً قابلًا للاستخدام مباشرةً.
/// </summary>
public static class LogoUrlExtensions
{
    /// <summary>يحوّل مساراً نسبياً للشعار إلى رابط مطلق كامل بمعرفة أصل الطلب.</summary>
    public static string? ToAbsoluteLogoUrl(this ControllerBase controller, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        // رابط مطلق بالفعل — لا نعيد بنائه
        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return relativePath;

        var origin = $"{controller.Request.Scheme}://{controller.Request.Host.Value}";
        return $"{origin}{relativePath}";
    }

    /// <summary>
    /// يعيد DTO بمعرّف <c>Logo</c> مطلقاً. التحويل في مكانه آمن لأن الخدمة
    /// تُنشئ نسخاً جديدة من FactoryDto لكل طلب (MapToDtoAsync).
    /// </summary>
    public static FactoryDto ToAbsoluteLogo(this ControllerBase controller, FactoryDto dto)
    {
        dto.Logo = controller.ToAbsoluteLogoUrl(dto.Logo);
        return dto;
    }

    /// <summary>يطبّق التحويل على جميع عناصر القائمة (FactoryDto).</summary>
    public static IReadOnlyList<FactoryDto> ToAbsoluteLogo(this ControllerBase controller, IEnumerable<FactoryDto> dtos)
    {
        foreach (var dto in dtos)
            controller.ToAbsoluteLogo(dto);
        return dtos.ToList();
    }
}