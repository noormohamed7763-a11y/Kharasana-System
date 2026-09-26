using Kharasana.Web.Models.ConcreteCatalog;
using Kharasana.Web.Services.Interfaces;

namespace Kharasana.Web.Services;

public class ConcreteCatalogService : IConcreteCatalogService
{
    private readonly List<ConcreteStandardModel> _catalog =
    [
        new() { Code = "C20", Strength = 20, Usage = "الأرضيات والممرات" },
        new() { Code = "C25", Strength = 25, Usage = "الأسقف" },
        new() { Code = "C30", Strength = 30, Usage = "الأعمدة والكمرات" },
        new() { Code = "C35", Strength = 35, Usage = "الجسور" },
        new() { Code = "C40", Strength = 40, Usage = "المنشآت الخاصة" },
        new() { Code = "C45", Strength = 45, Usage = "الأحمال العالية" },
        new() { Code = "C50", Strength = 50, Usage = "المشاريع الكبرى" }
    ];

    public List<ConcreteStandardModel> GetAll()
    {
        return _catalog;
    }

    public ConcreteStandardModel? GetByCode(string code)
    {
        return _catalog.FirstOrDefault(x =>
            x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}