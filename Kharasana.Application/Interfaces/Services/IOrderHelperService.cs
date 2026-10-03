using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Interfaces.Services;

/// <summary>
/// واجهة الخدمات المساعدة المشتركة لعمليات الطلبات.
/// </summary>
public interface IOrderHelperService
{
    Task<Order> GetOrderOrThrowAsync(int id, int callerId, UserRole callerRole, int? callerFactoryId);
    void ReleaseDriver(Order order);
}
