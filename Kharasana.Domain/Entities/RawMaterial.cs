using Kharasana.Domain.Common;
using Kharasana.Domain.Entities;

namespace Kharasana.Domain.Entities;

public class RawMaterial : TenantEntity
{
    public int RawMaterialId { get; set; }

    public string Name { get; set; } = null!;

    public decimal CurrentStock { get; set; }

    public decimal MinStockLevel { get; set; }

    public string Unit { get; set; } = null!; // مثال: "طن", "متر مكعب"

    public virtual Factory Factory { get; set; } = null!;
}
