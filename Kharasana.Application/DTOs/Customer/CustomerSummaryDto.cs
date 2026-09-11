namespace Kharasana.Application.DTOs.Customer;

public class CustomerSummaryDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public DateTime? LastOrderDate { get; set; }
}