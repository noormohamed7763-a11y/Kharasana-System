using Kharasana.Domain.Entities;

namespace Kharasana.Application.Interfaces.Repositories;

public interface IActivationTokenRepository : IGenericRepository<ActivationToken>
{
    Task<ActivationToken?> GetByHashAsync(string hash);
}