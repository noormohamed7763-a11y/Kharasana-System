using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class PhoneOrderDto
{
    // بيانات العميل — يُبحث عنه بالهاتف، وإن لم يوجد يُنشأ تلقائيًا
    public string ClientPhone { get; set; } = string.Empty;
    public string? ClientFullName { get; set; } // إلزامي فقط إذا كان عميلاً جديدًا

    // بيانات الطلب
    public int FactoryId { get; set; }
    public int ConcreteTypeId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectOwnerName { get; set; }
    public string? SiteArea { get; set; }
    public string? SiteDescription { get; set; }
    public SlabType SlabType { get; set; }
    public decimal Quantity { get; set; }
    public bool NeedPump { get; set; }
    public int? FloorNumber { get; set; }
    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public string? Notes { get; set; }
}