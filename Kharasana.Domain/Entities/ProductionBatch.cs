using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Domain.Entities;

public class ProductionBatch : TenantEntity
{
    public int ProductionBatchId { get; set; }

    public int OrderId { get; set; }

    public int ConcreteTypeId { get; set; }

    public decimal Quantity { get; set; }

    public ProductionStatus Status { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual ConcreteType ConcreteType { get; set; } = null!;
    public virtual Factory Factory { get; set; } = null!;
}
