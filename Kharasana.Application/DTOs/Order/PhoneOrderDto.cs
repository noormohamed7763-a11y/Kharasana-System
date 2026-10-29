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
    public string? Landmarks { get; set; }
    public string? SupervisorName { get; set; }
    public string? SupervisorPhone { get; set; }
    public CementType CementType { get; set; } = CementType.Ordinary;
    public AggregateSize AggregateSize { get; set; } = AggregateSize.Size20mm;
    public Admixture Admixture { get; set; } = Admixture.Plasticizer;
    public SlabType SlabType { get; set; }
    public decimal Quantity { get; set; }
    public bool NeedPump { get; set; }
    public int? FloorNumber { get; set; }
    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }
    public string? Notes { get; set; }
}
