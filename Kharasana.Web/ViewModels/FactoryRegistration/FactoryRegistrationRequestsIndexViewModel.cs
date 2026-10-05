using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Shared;

namespace Kharasana.Web.ViewModels.FactoryRegistration;

public class FactoryRegistrationRequestsIndexViewModel
{
    public PagedResult<RegistrationRequestListItemViewModel>? PagedRequests { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? StatusFilter { get; set; }

    // Summary Counts
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}