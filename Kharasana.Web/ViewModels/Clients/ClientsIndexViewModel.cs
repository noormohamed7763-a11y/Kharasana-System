using Kharasana.Application.Common;

namespace Kharasana.Web.ViewModels.Clients;

public class ClientsIndexViewModel
{
    public PagedResult<ClientListItemViewModel>? PagedClients { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}