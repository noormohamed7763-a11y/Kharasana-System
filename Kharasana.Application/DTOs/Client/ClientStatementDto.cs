using Kharasana.Application.DTOs.Order;

namespace Kharasana.Application.DTOs.Client;

public class ClientStatementDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime StatementDate { get; set; }

    public int TotalOrders { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }

    public IReadOnlyList<ClientStatementItemDto> LedgerItems { get; set; } = new List<ClientStatementItemDto>();

    // للرجوع السريع للطلبات إذا لزم الأمر في العرض
    public IReadOnlyList<OrderDto> Orders { get; set; } = new List<OrderDto>();
}

public class ClientStatementItemDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }

    /// <summary>المبلغ المدين (قيمة الطلب الجديد)</summary>
    public decimal Debit { get; set; }

    /// <summary>المبلغ الدائن (دفعة مسددة)</summary>
    public decimal Credit { get; set; }

    public decimal RunningBalance { get; set; }

    /// <summary>رقم الطلب المرتبط (إن وجد)</summary>
    public string? OrderNumber { get; set; }

    /// <summary>معرف الطلب (للرابط)</summary>
    public int? OrderId { get; set; }
}
