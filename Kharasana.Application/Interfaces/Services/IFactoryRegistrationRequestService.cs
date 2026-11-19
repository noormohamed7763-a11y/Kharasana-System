using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Factory;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

public interface IFactoryRegistrationRequestService
{
    Task<ServiceResult<int>> CreateRequestAsync(RegisterFactoryDto dto);
    Task<IEnumerable<FactoryRegistrationRequest>> GetAllRequestsAsync();
    Task<FactoryRegistrationRequest?> GetRequestDetailsAsync(int requestId);
    Task<IEnumerable<FactoryRegistrationRequest>> GetPendingRequestsAsync();
    Task<ServiceResult<ApproveResult>> ApproveRequestAsync(int requestId);
    Task<ServiceResult> RejectRequestAsync(int requestId, string reason);

    /// <summary>
    /// جلب طلبات التسجيل مع دعم الترقيم والبحث والفلترة.
    /// </summary>
    Task<PagedResult<FactoryRegistrationRequest>> GetPagedRequestsAsync(RegistrationRequestFilterParams filter);

    Task<(int Pending, int Approved, int Rejected)> GetStatusCountsAsync();
}

public class ApproveResult
{
    public string ActivationTokenHash { get; set; } = string.Empty;
    public string ActivationUrl { get; set; } = string.Empty;
}