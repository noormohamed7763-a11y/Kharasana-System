namespace Kharasana.Domain.Common;

/// <summary>
/// كيان أساسي للميزات الجديدة يضمن توحيد حقول التدقيق والتحكم في النسخ.
/// </summary>
public abstract class BaseEntity
{
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// كيان يدعم عزل المصانع (Multi-Tenancy) للميزات الجديدة.
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public int FactoryId { get; set; }
}
