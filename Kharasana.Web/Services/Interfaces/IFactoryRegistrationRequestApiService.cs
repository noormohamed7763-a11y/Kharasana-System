using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Entities;

namespace Kharasana.Web.Services.Interfaces;

public interface IFactoryRegistrationRequestApiService
{
    Task<IEnumerable<FactoryRegistrationRequest>> GetAllAsync();
    Task<int> GetPendingRequestsCountAsync();
    Task<int> GetApprovedRequestsCountAsync();
    Task<int> GetRejectedRequestsCountAsync();
    Task<PagedResult<FactoryRegistrationRequest>> GetPagedRequestsAsync(RegistrationRequestFilterParams filter);
    Task<FactoryRegistrationRequest?> GetByIdAsync(int id);
    Task<ServiceResult> RegisterAsync(RegisterFactoryDto dto);
    Task<ServiceResult> ApproveAsync(int id);
    Task<ServiceResult> RejectAsync(int id, string reason);
}