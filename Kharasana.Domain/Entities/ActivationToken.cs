namespace Kharasana.Domain.Entities;

public class ActivationToken
{
    public int ActivationTokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // Navigation Properties
    public User User { get; set; } = null!;
}
