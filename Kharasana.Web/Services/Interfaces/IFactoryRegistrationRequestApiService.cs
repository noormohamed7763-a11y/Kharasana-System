using Kharasana.Application.DTOs.Factory;
using Kharasana.Application.Common;
using Kharasana.Domain.Entities;

namespace Kharasana.Web.Services.Interfaces;

public interface IFactoryRegistrationRequestApiService
{
    Task<IEnumerable<FactoryRegistrationRequest>> GetAllAsync();
    Task<IEnumerable<FactoryRegistrationRequest>> GetPendingAsync();
    Task<FactoryRegistrationRequest?> GetByIdAsync(int id);
    Task<ServiceResult> RegisterAsync(RegisterFactoryDto dto);
    Task<ServiceResult> ApproveAsync(int id);
    Task<ServiceResult> RejectAsync(int id, string reason);
}