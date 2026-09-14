using System;
using System.Text.Json.Serialization;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.ViewModels.Orders;

public class OrderDto
{
    // ============================================================
    // BASIC INFO
    // ============================================================
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }


    // ============================================================
    // FACTORY INFO
    // ============================================================
    [JsonPropertyName("factoryId")]
    public int FactoryId { get; set; }

    [JsonPropertyName("factoryName")]
    public string FactoryName { get; set; } = string.Empty;


    // ============================================================
    // CLIENT INFO
    // ============================================================
    [JsonPropertyName("clientId")]
    public int ClientId { get; set; }

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;

    [JsonPropertyName("clientPhone")]
    public string? ClientPhone { get; set; }

    [JsonPropertyName("clientOrdersCount")]
    public int? ClientOrdersCount { get; set; }


    // ============================================================
    // CONCRETE INFO
    // ============================================================
    [JsonPropertyName("concreteTypeId")]
    public int ConcreteTypeId { get; set; }

    [JsonPropertyName("concreteTypeName")]
    public string ConcreteTypeName { get; set; } = string.Empty;

    [JsonPropertyName("concreteStrength")]
    public int ConcreteStrength { get; set; }


    // ============================================================
    // PRICING INFO
    // ============================================================
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal? TotalPrice { get; set; }


    // ============================================================
    // PROJECT INFO
    // ============================================================
    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("projectOwnerName")]
    public string? ProjectOwnerName { get; set; }

    [JsonPropertyName("siteArea")]
    public string? SiteArea { get; set; }

    [JsonPropertyName("siteDescription")]
    public string? SiteDescription { get; set; }

    [JsonPropertyName("slabType")]
    public int SlabType { get; set; }

    [JsonPropertyName("slabTypeDisplay")]
    public string? SlabTypeDisplay { get; set; }


    // ============================================================
    // TRANSPORT INFO
    // ============================================================
    [JsonPropertyName("transportMethod")]
    public TransportMethod TransportMethod { get; set; }

    [JsonPropertyName("transportMethodDisplay")]
    public string? TransportMethodDisplay { get; set; }

    [JsonPropertyName("needPump")]
    public bool NeedPump { get; set; }

    [JsonPropertyName("floorNumber")]
    public int? FloorNumber { get; set; }

    [JsonPropertyName("pouringDate")]
    public DateTime? PouringDate { get; set; }


    // ============================================================
    // DRIVER INFO
    // ============================================================
    [JsonPropertyName("driverId")]
    public int? DriverId { get; set; }

    [JsonPropertyName("driverName")]
    public string DriverName { get; set; } = string.Empty;

    [JsonPropertyName("truckPlate")]
    public string? TruckPlate { get; set; }


    // ============================================================
    // STATUS INFO
    // ============================================================
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    [JsonPropertyName("statusDisplay")]
    public string StatusDisplay { get; set; } = string.Empty;

    [JsonPropertyName("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    [JsonPropertyName("closedAt")]
    public DateTime? ClosedAt { get; set; }


    // ============================================================
    // NOTES
    // ============================================================
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }


    // ============================================================
    // HELPER PROPERTIES
    // ============================================================

    public string StatusArabic => Status switch
    {
        OrderStatus.New => "جديد",
        OrderStatus.Pending => "⏳ قيد الانتظار",
        OrderStatus.Approved => "✅ معتمد",
        OrderStatus.Rejected => "❌ مرفوض",
        OrderStatus.Cancelled => "🚫 ملغي",
        OrderStatus.OnTheWay => "🚚 في الطريق",
        OrderStatus.Delivered => "📦 تم التسليم",
        OrderStatus.Closed => "🔒 مغلق",
        _ => "غير معروف"
    };

    public int StatusInt => (int)Status;

    /// <summary>
    /// تاريخ الصب بصيغة dd/MM/yyyy جاهزة للعرض
    /// </summary>
    public string PouringDateDisplay => PouringDate?.ToString("dd/MM/yyyy") ?? "-";

    /// <summary>
    /// هل تاريخ الصب هو اليوم؟
    /// </summary>
    public bool IsPouringDateToday => PouringDate.HasValue && PouringDate.Value.Date == DateTime.Today;

    /// <summary>
    /// جزء التاريخ من رقم الطلب (ORD-yyyyMMdd-seq → yyyy-MM-dd)
    /// </summary>
    public string OrderNumberDate
    {
        get
        {
            var parts = OrderNumber.Split('-');
            if (parts.Length >= 3 && parts[1].Length == 8)
            {
                return $"{parts[1][..4]}-{parts[1][4..6]}-{parts[1][6..8]}";
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// الجزء التسلسلي من رقم الطلب (ORD-yyyyMMdd-seq → seq)
    /// </summary>
    public string OrderNumberSeq
    {
        get
        {
            var parts = OrderNumber.Split('-');
            return parts.Length >= 3 ? parts[2] : OrderNumber;
        }
    }
}