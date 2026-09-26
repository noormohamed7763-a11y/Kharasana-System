namespace Kharasana.Application.DTOs.Customer;

public class CustomerSummaryDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>بيانات حساب العميل — تُعرض في صفحة التفاصيل، وهي هنا لأنها تُقرأ من الرسم نفسه.</summary>
    public string? Email { get; set; }

    public string? WhatsApp { get; set; }

    /// <summary>حالة الحساب — بلاها تعرض صفحة التفاصيل «موقوف» لكل عميل.</summary>
    public bool IsActive { get; set; }

    public int OrdersCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public DateTime? LastOrderDate { get; set; }
}