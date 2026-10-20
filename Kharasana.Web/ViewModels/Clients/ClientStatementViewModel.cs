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

    public DateTime StatementDate { get; set; } = DateTime.Now;

    public int TotalOrders { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    public IReadOnlyList<ClientStatementItemViewModel> LedgerItems { get; set; } = Array.Empty<ClientStatementItemViewModel>();
    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();
}

public class ClientStatementItemViewModel
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public string? OrderNumber { get; set; }
    public int? OrderId { get; set; }
}
