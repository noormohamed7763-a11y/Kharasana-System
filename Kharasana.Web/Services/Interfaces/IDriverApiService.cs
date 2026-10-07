using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Drivers;
using Kharasana.Domain.Enums;

namespace Kharasana.Web.Services.Interfaces;

/// <summary>
/// واجهة خدمات السائقين - مسؤولة عن التواصل مع الـ API الخاص بالسائقين
/// </summary>
public interface IDriverApiService
{
    Task<PagedResult<DriverListItemViewModel>> GetDriversAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null,
        CancellationToken cancellationToken = default);

    Task<DriverListItemViewModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<EditDriverViewModel> GetForEditAsync(int driverId, CancellationToken cancellationToken = default);

    Task CreateAsync(CreateDriverViewModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, EditDriverViewModel model, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(int id, UpdateDriverStatusViewModel model, CancellationToken cancellationToken = default);

    Task<(int Available, int Busy, int Offline)> GetStatusCountsAsync(
        string? search,
        int? factoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ToggleActiveAsync(int id, CancellationToken cancellationToken = default);
}