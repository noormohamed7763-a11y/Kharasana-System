using Kharasana.Web.ViewModels.ConcreteTypes;

namespace Kharasana.Web.Services.Interfaces;

public interface IConcreteTypeApiService
{
    Task<List<ConcreteTypeListItemViewModel>> GetAllAsync();

    Task<ConcreteTypeViewModel?> GetByIdAsync(int id);

    Task<bool> CreateAsync(CreateConcreteTypeViewModel model);

    Task<bool> UpdateAsync(int id, UpdateConcreteTypeViewModel model);

    Task<bool> DeleteAsync(int id);
}