namespace Kharasana.Application.Interfaces.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    int? FactoryId { get; }
    bool IsInRole(string roleName);
}
