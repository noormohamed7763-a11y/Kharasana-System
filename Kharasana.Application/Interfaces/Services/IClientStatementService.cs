using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Client;

namespace Kharasana.Application.Interfaces.Services;

public interface IClientStatementService
{
    Task<ClientStatementDto> GetClientStatementAsync(int clientId, CallerContext caller);
}
