using Kharasana.Web.ViewModels.Shared;

public interface ILookupApiService
{
    Task<List<LookupDto>> GetClientsAsync();
    Task<List<LookupDto>> GetDriversAsync();
    Task<List<LookupDto>> GetAvailableDriversAsync();
    Task<List<LookupDto>> GetAvailableDriversByFactoryAsync(int factoryId); // ✅ إضافة هذه الدالة
    Task<List<LookupDto>> GetConcreteTypesAsync();
}