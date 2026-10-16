using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IFactoryRegistrationRequestService
{
    Task<ServiceResult> CreateRequestAsync(RegisterFactoryDto dto);
    Task<IEnumerable<FactoryRegistrationRequest>> GetAllRequestsAsync();
    Task<FactoryRegistrationRequest?> GetRequestDetailsAsync(int requestId);
    Task<IEnumerable<FactoryRegistrationRequest>> GetPendingRequestsAsync();
    Task<ServiceResult> ApproveRequestAsync(int requestId);
    Task<ServiceResult> RejectRequestAsync(int requestId, string reason);

    /// <summary>
    /// جلب طلبات التسجيل مع دعم الترقيم والبحث والفلترة.
    /// </summary>
    Task<PagedResult<FactoryRegistrationRequest>> GetPagedRequestsAsync(RegistrationRequestFilterParams filter);
}