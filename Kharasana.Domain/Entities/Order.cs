using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class Order
{
    public int OrderId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public string OrderNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }

    public int FactoryId { get; set; }

    public int ConcreteTypeId { get; set; }

    public string? ProjectName { get; set; }

    public string? ProjectOwnerName { get; set; }

    public string? SiteArea { get; set; }

    public string? SiteDescription { get; set; }

    /// <summary>
    /// إحداثيات موقع صبّ الخرسانة كما التقطها جهاز العميل عند إنشاء الطلب.
    ///
    /// <para>قابلان للفراغ: الطلبات المنشأة قبل إضافة GPS، وطلبات السائقين
    /// والموظفين التي لا تمرّ بتطبيق العميل، تبقى صالحة بلا موقع.</para>
    /// </summary>
    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public SlabType SlabType { get; set; }

    public decimal Quantity { get; set; }

    public bool NeedPump { get; set; }

    public int? FloorNumber { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsManuallyPriced { get; set; } = false;

    public decimal TotalPrice { get; set; }

    public decimal TotalDiscountAmount { get; set; }

    public decimal NetAmount { get; set; }

    // ✅ لقطتان تاريخيتان لنوع الخرسانة وقت إنشاء الطلب — على نمط UnitPrice أعلاه.
    //    بدونهما كان تغيير اسم النوع أو مقاومته في الكتالوج يُعيد كتابة عرض الطلبات
    //    القديمة: طلب سُعِّر وبِيع باسم «C30» يظهر لاحقًا باسم النوع الجديد.
    //    قابلتان للفراغ كي تبقى صفوف ما قبل الترحيل صالحة، ويسقط العرض عند غيابهما
    //    إلى النوع الحالي (انظر MapToOrderDto/MapToDetailsDto).
    public string? ConcreteTypeNameSnapshot { get; set; }

    public int? ConcreteTypeStrengthSnapshot { get; set; }

    public DateTime? PouringDate { get; set; }
    public TransportMethod TransportMethod { get; set; }

    public int? DriverId { get; set; }

    public string? TruckPlate { get; set; }

    public OrderStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    
    public virtual ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();

    /// <summary>
    /// يعيد حساب صافي المبلغ المستحق = الإجمالي − الخصم.
    ///
    /// <para><b>لماذا دالة على الكيان:</b> <c>NetAmount</c> مشتقّ من <c>TotalPrice</c>
    /// و<code>TotalDiscountAmount</code>، وأي مسار يغيّر أحدهما <b>يجب</b> أن يعيد حساب
    /// الآخر. لو ترك كل مسار يعيّن <c>TotalPrice</c> وحده، بقي <c>NetAmount</c> صفراً
    /// (قيمته الافتراضية في قاعدة البيانات) بينما <c>RemainingAmount = NetAmount − PaidAmount</c>
    /// فيعرض رصيداً سالباً كاذباً على العميل.</para>
    ///
    /// <para>استُدعيت في كل موضع يعيّن <c>TotalPrice</c>: الإنشاء، والتعديل، والتسعير.</para>
    /// </summary>
    public void RecalculateNetAmount()
    {
        NetAmount = TotalPrice - TotalDiscountAmount;
    }

    public User Client { get; set; } = null!;

    public User? Driver { get; set; }

    public Factory Factory { get; set; } = null!;

    public ConcreteType ConcreteType { get; set; } = null!;
}