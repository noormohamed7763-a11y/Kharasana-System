namespace Kharasana.Web.ViewModels.Clients;

public class ClientDetailsViewModel
{
    // ============================================================
    // BASIC INFORMATION
    // ============================================================

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    public string? ProfileImage { get; set; }

    public bool IsActive { get; set; }


    // ============================================================
    // CLIENT ORDER STATISTICS
    // ============================================================

    public int OrdersCount { get; set; }

    public decimal TotalQuantity { get; set; }

    public DateTime? LastOrderDate { get; set; }
}