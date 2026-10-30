using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; } // سبب التغيير (مثل سبب الرفض أو الإلغاء)

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual User ChangedBy { get; set; } = null!;
}
