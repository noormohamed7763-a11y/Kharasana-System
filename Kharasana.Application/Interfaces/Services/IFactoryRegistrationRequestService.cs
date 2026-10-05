using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Common;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Services;

public interface IFactoryRegistrationRequestService
{
    Task<ServiceResult> CreateRequestAsync(RegisterFactoryDto dto);
    Task<IEnumerable<FactoryRegistrationRequest>> GetAllRequestsAsync();
    Task<FactoryRegistrationRequest?> GetRequestDetailsAsync(int requestId);
    Task<IEnumerable<FactoryRegistrationRequest>> GetPendingRequestsAsync();
    Task<ServiceResult> ApproveRequestAsync(int requestId);
    Task<ServiceResult> RejectRequestAsync(int requestId, string reason);
}