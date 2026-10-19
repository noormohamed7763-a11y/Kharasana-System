using Kharasana.Web.ViewModels.Orders;

namespace Kharasana.Web.ViewModels.Clients;

/// <summary>
/// نموذج كشف حساب صبات العميل — يُعرض كوثيقة رسمية مطابقة قابلة للطباعة والمشاركة عبر الواتساب.
/// </summary>
public class ClientStatementViewModel
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }

    public int? FactoryId { get; set; }
    public string FactoryName { get; set; } = string.Empty;

    public DateTime StatementDate { get; set; } = DateTime.Now;

    public int TotalOrders { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }

    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();
}
