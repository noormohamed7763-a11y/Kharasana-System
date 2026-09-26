using Kharasana.Web.ViewModels.Shared;

public interface ILookupApiService
{
    Task<List<LookupDto>> GetClientsAsync();
    Task<List<LookupDto>> GetAvailableDriversAsync();
    Task<List<LookupDto>> GetConcreteTypesAsync();
}