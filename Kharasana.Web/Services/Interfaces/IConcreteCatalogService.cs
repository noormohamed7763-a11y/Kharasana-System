using Kharasana.Web.Models.ConcreteCatalog;

namespace Kharasana.Web.Services.Interfaces;

public interface IConcreteCatalogService
{
    List<ConcreteStandardModel> GetAll();

    ConcreteStandardModel? GetByCode(string code);
}