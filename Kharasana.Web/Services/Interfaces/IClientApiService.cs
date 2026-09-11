using Kharasana.Application.Common;
using Kharasana.Web.ViewModels.Clients;

namespace Kharasana.Web.Services.Interfaces;

public interface IClientApiService
{
    // ============================================================
    // GET CLIENTS
    // ============================================================
    Task<PagedResult<ClientListItemViewModel>?> GetClientsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null);

    // ============================================================
    // GET DETAILS
    // ============================================================
    Task<ClientDetailsViewModel?> GetDetailsAsync(int clientId);

    // ============================================================
    // CREATE
    // ============================================================
    Task<bool> CreateAsync(CreateClientViewModel model);

    // ============================================================
    // GET FOR EDIT
    // ============================================================
    Task<EditClientViewModel?> GetForEditAsync(int clientId);

    // ============================================================
    // UPDATE
    // ============================================================
    Task<bool> UpdateAsync(int id, EditClientViewModel model);
}