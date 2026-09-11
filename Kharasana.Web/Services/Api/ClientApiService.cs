using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Clients;
using Kharasana.Application.DTOs.Customer;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api;

public class ClientApiService : IClientApiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ClientApiService> _logger;

    public ClientApiService(
        ApiClient apiClient,
        ILogger<ClientApiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // ============================================================
    // GET CLIENTS
    // ============================================================
    public async Task<PagedResult<ClientListItemViewModel>?> GetClientsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        int? factoryId = null)
    {
        try
        {
            var query =
                $"Orders/customers?PageNumber={pageNumber}&PageSize={pageSize}";

            if (!string.IsNullOrWhiteSpace(search))
            {
                query +=
                    $"&Search={Uri.EscapeDataString(search)}";
            }

            if (factoryId.HasValue)
            {
                query += $"&FactoryId={factoryId.Value}";
            }

            var response =
                await _apiClient.GetAsync<
                    ApiResponse<PagedResult<CustomerSummaryDto>>
                >(query);

            if (response == null ||
                !response.Success ||
                response.Data == null)
            {
                _logger.LogWarning(
                    "GetClientsAsync failed. Query: {Query}",
                    query);

                return null;
            }

            return new PagedResult<ClientListItemViewModel>
            {
                PageNumber = response.Data.PageNumber,
                PageSize = response.Data.PageSize,
                TotalCount = response.Data.TotalCount,

                Items = response.Data.Items
                    .Select(c => new ClientListItemViewModel
                    {
                        UserId = c.UserId,
                        FullName = c.FullName,
                        Phone = c.Phone,
                        OrdersCount = c.OrdersCount,
                        TotalQuantity = c.TotalQuantity,
                        LastOrderDate = c.LastOrderDate
                    })
                    .ToList()
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in GetClientsAsync");

            return null;
        }
    }


    // ============================================================
    // GET DETAILS
    // ============================================================
    public async Task<ClientDetailsViewModel?> GetDetailsAsync(
        int clientId)
    {
        try
        {
            // نستخدم نفس مصدر قائمة العملاء.
            // لا نستخدم Users/{id} لأن العميل هنا
            // مستخرج من تعاملاته مع المصنع.
            var response =
                await _apiClient.GetAsync<
                    ApiResponse<PagedResult<CustomerSummaryDto>>
                >(
                    "Orders/customers?PageNumber=1&PageSize=100"
                );

            if (response == null ||
                !response.Success ||
                response.Data == null ||
                response.Data.Items == null)
            {
                _logger.LogWarning(
                    "GetDetailsAsync: Could not load customers.");

                return null;
            }

            // البحث عن العميل المطلوب
            var customer =
                response.Data.Items
                    .FirstOrDefault(c => c.UserId == clientId);

            if (customer == null)
            {
                _logger.LogWarning(
                    "GetDetailsAsync: Customer {ClientId} not found.",
                    clientId);

                return null;
            }

            // بناء بيانات صفحة التفاصيل
            return new ClientDetailsViewModel
            {
                UserId = customer.UserId,
                FullName = customer.FullName,
                Phone = customer.Phone,

                OrdersCount = customer.OrdersCount,
                TotalQuantity = customer.TotalQuantity,
                LastOrderDate = customer.LastOrderDate
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in GetDetailsAsync for clientId={ClientId}",
                clientId);

            return null;
        }
    }


    // ============================================================
    // CREATE CLIENT
    // ============================================================
    public async Task<bool> CreateAsync(
        CreateClientViewModel model)
    {
        try
        {
            var payload = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Phone = model.Phone,
                WhatsApp = model.WhatsApp,

                // Client
                Role = 3
            };

            var response =
                await _apiClient.PostAsync<ApiResponse<object>>(
                    "Users",
                    payload);

            if (response == null ||
                !response.Success)
            {
                _logger.LogWarning(
                    "CreateAsync failed. Message: {Message}",
                    response?.Message);

                return false;
            }

            return true;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in CreateAsync");

            return false;
        }
    }


    // ============================================================
    // GET CLIENT FOR EDIT
    // ============================================================
    public async Task<EditClientViewModel?> GetForEditAsync(
        int clientId)
    {
        try
        {
            var response =
                await _apiClient.GetAsync<
                    ApiResponse<ClientAccountDto>
                >($"Users/{clientId}");

            if (response == null ||
                !response.Success ||
                response.Data == null)
            {
                _logger.LogWarning(
                    "GetForEditAsync: Client {ClientId} not found.",
                    clientId);

                return null;
            }

            var account = response.Data;

            return new EditClientViewModel
            {
                UserId = account.UserId,
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Phone,
                WhatsApp = account.WhatsApp,
                ProfileImage = account.ProfileImage,
                IsActive = account.IsActive
            };
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in GetForEditAsync for clientId={ClientId}",
                clientId);

            return null;
        }
    }


    // ============================================================
    // UPDATE CLIENT
    // ============================================================
    public async Task<bool> UpdateAsync(
        int id,
        EditClientViewModel model)
    {
        try
        {
            var payload = new
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                WhatsApp = model.WhatsApp,
                ProfileImage = model.ProfileImage,
                IsActive = model.IsActive,

                // Client
                Role = 3,

                // العميل لا يتبع مصنعًا
                FactoryId = (int?)null,

                // ليس سائقًا
                LicenseNumber = (string?)null,
                DriverStatus = (int?)null
            };

            var response =
                await _apiClient.PutAsync<ApiResponse<object>>(
                    $"Users/{id}",
                    payload);

            if (response == null ||
                !response.Success)
            {
                _logger.LogWarning(
                    "UpdateAsync failed for client {Id}. Message: {Message}",
                    id,
                    response?.Message);

                return false;
            }

            return true;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception in UpdateAsync for client {Id}",
                id);

            return false;
        }
    }
}


// ============================================================
// INTERNAL DTO
// GET /api/Users/{id}
// ============================================================
internal class ClientAccountDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? WhatsApp { get; set; }

    public string? ProfileImage { get; set; }

    public bool IsActive { get; set; }
}
