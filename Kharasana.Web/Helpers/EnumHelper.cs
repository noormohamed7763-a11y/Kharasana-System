using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Kharasana.Web.Helpers;

public static class EnumHelper
{
    /// <summary>
    /// الحصول على النص المعروض للقيمة (يدعم DisplayAttribute)
    /// </summary>
    public static string GetDisplayName(this Enum? value)
    {
        if (value == null) return string.Empty;

        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = field.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }

    /// <summary>
    /// الحصول على قائمة SelectListItems مع خيار افتراضي
    /// </summary>
    public static List<SelectListItem> GetSelectList<TEnum>(bool addDefaultOption = true, string? defaultText = null) where TEnum : Enum
    {
        var items = Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(e => new SelectListItem
            {
                Value = Convert.ToInt32(e).ToString(),
                Text = GetDisplayName(e)
            })
            .ToList();

        if (addDefaultOption)
        {
            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = defaultText ?? $"-- اختر --",
                Selected = true
            });
        }

        return items;
    }

    /// <summary>
    /// الحصول على قائمة SelectListItems لـ SlabType
    /// </summary>
    public static List<SelectListItem> GetSlabTypeSelectList(bool addDefaultOption = true)
    {
        return GetSelectList<SlabType>(addDefaultOption, "-- اختر نوع البلاطة --");
    }

    /// <summary>
    /// الحصول على قائمة SelectListItems لـ TransportMethod
    /// </summary>
    public static List<SelectListItem> GetTransportMethodSelectList(bool addDefaultOption = true)
    {
        return GetSelectList<TransportMethod>(addDefaultOption, "-- اختر طريقة النقل --");
    }
}
