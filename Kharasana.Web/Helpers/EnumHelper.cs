using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Kharasana.Web.Helpers;

public static class EnumHelper
{
    /// <summary>
    /// الحصول على النص المعروض للقيمة (يدعم DisplayAttribute)
    /// </summary>
    public static string GetDisplayName(Enum? value)  // ✅ إضافة ? لجعلها Nullable
    {
        if (value == null) return string.Empty;

        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = field.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }

    /// <summary>
    /// الحصول على وصف القيمة (يدعم DisplayAttribute)
    /// </summary>
    public static string GetDescription(Enum? value)  // ✅ إضافة ? لجعلها Nullable
    {
        if (value == null) return string.Empty;

        var field = value.GetType().GetField(value.ToString());
        if (field == null) return string.Empty;

        var attribute = field.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Description ?? string.Empty;
    }

    /// <summary>
    /// الحصول على قائمة SelectListItems مع خيار افتراضي
    /// </summary>
    public static List<SelectListItem> GetSelectList<TEnum>(bool addDefaultOption = true, string? defaultText = null) where TEnum : Enum  // ✅ إضافة ? لـ defaultText
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
    /// الحصول على أيقونة Bootstrap لكل قيمة (ما عدا OrderStatus)
    /// </summary>
    public static string GetIcon(Enum? value)  // ✅ إضافة ? لجعلها Nullable
    {
        if (value == null) return "bi-question-circle";

        return value switch
        {
            // TransportMethod
            TransportMethod.FactoryTransport => "bi-truck",
            TransportMethod.ClientOwnTransport => "bi-car-front",

            // SlabType
            SlabType.Foundation => "bi-grid-1x2",
            SlabType.Columns => "bi-grid-3x3-gap",
            SlabType.Beams => "bi-grid-3x3",
            SlabType.Roof => "bi-grid",
            SlabType.Other => "bi-grid-3x3-gap-fill",

            _ => "bi-question-circle"
        };
    }

    /// <summary>
    /// الحصول على كلاس Bootstrap للشارة (ما عدا OrderStatus)
    /// </summary>
    public static string GetBadgeClass(Enum? value)  // ✅ إضافة ? لجعلها Nullable
    {
        if (value == null) return "bg-secondary";

        return value switch
        {
            // TransportMethod
            TransportMethod.FactoryTransport => "bg-primary",
            TransportMethod.ClientOwnTransport => "bg-success",

            // SlabType
            SlabType.Foundation => "bg-info",
            SlabType.Columns => "bg-primary",
            SlabType.Beams => "bg-warning",
            SlabType.Roof => "bg-success",
            SlabType.Other => "bg-secondary",

            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// الحصول على كلاس CSS إضافي (ما عدا OrderStatus)
    /// </summary>
    public static string GetCssClass(Enum? value)  // ✅ إضافة ? لجعلها Nullable
    {
        if (value == null) return "default";

        return value switch
        {
            // TransportMethod
            TransportMethod.FactoryTransport => "transport-factory",
            TransportMethod.ClientOwnTransport => "transport-client",

            // SlabType
            SlabType.Foundation => "slab-foundation",
            SlabType.Columns => "slab-columns",
            SlabType.Beams => "slab-beams",
            SlabType.Roof => "slab-roof",
            SlabType.Other => "slab-other",

            _ => "default"
        };
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