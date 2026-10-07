using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Clients;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net;

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
                $"Orders/customers?PageNumber={pageNumber}&PageSize={pageSize}&SortBy=Id&Order=Desc";

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
                    "فشل GetClientsAsync. الاستعلام: {Query}",
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
                "استثناء في GetClientsAsync");

            return null;
        }
    }


    // ============================================================
    // GET CLIENT STATS
    // ============================================================
    public async Task<ClientStatsViewModel?> GetClientStatsAsync(
        int? factoryId = null)
    {
        try
        {
            var query = "Orders/customers/stats";

            if (factoryId.HasValue)
            {
                query += $"?FactoryId={factoryId.Value}";
            }

            var response =
                await _apiClient.GetAsync<ApiResponse<ClientStatsViewModel>>(query);

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("فشل GetClientStatsAsync.");
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء في GetClientStatsAsync");
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
            // ✅ نقطة عميل واحد على الخادم.
            //    كان يجلب أول 100 عميل (PageSize=100) ثم يبحث فيهم محلياً، فيُبلَّغ عن
            //    كل عميل يقع بعد العنصر المئة أنه «غير موجود» وهو موجود فعلاً.
            //    ونفس النداء يعيد Email/WhatsApp/IsActive فتُعرض بدل «-» و«موقوف».
            var response =
                await _apiClient.GetAsync<
                    ApiResponse<CustomerSummaryDto>
                >(
                    $"Orders/customers/{clientId}"
                );

            if (response == null ||
                !response.Success ||
                response.Data == null)
            {
                _logger.LogWarning(
                    "GetDetailsAsync: تعذّر جلب العميل {ClientId}.",
                    clientId);

                throw new ApiServiceException(
                    HttpStatusCode.NotFound,
                    ApiErrorCatalog.ClientNotFound,
                    new object[] { clientId });
            }

            var customer = response.Data;

            // بناء بيانات صفحة التفاصيل
            return new ClientDetailsViewModel
            {
                UserId = customer.UserId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                WhatsApp = customer.WhatsApp,
                IsActive = customer.IsActive,

                OrdersCount = customer.OrdersCount,
                TotalQuantity = customer.TotalQuantity,
                LastOrderDate = customer.LastOrderDate
            };
        }
        catch (ApiServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "GetDetailsAsync: العميل {ClientId} غير موجود.",
                clientId);

            throw new ApiServiceException(
                HttpStatusCode.NotFound,
                ApiErrorCatalog.ClientNotFound,
                new object[] { clientId });
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء في GetDetailsAsync للعميل {ClientId}", clientId);
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
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
                Role = (int)UserRole.Client
            };

            var response =
                await _apiClient.PostAsync<ApiResponse<object>>(
                    "Users",
                    payload);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("فشل CreateAsync. الرسالة: {Message}", response?.Message);
                throw new ApiServiceException(
                    HttpStatusCode.BadRequest,
                    ApiErrorCatalog.ClientCreateFailed,
                    new object[] { response?.Message ?? "سبب غير معروف" });
            }

            return true;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء في CreateAsync");
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
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
                    ApiResponse<UserDto>
                >($"Users/{clientId}");

            if (response == null || !response.Success || response.Data == null)
            {
                _logger.LogWarning("GetForEditAsync: العميل {ClientId} غير موجود.", clientId);
                throw new ApiServiceException(
                    HttpStatusCode.NotFound,
                    ApiErrorCatalog.ClientNotFound,
                    new object[] { clientId });
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
            _logger.LogError(ex, "استثناء في GetForEditAsync للعميل {ClientId}", clientId);
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
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
                Role = (int)UserRole.Client,

                // العميل لا يتبع مصنعًا
                FactoryId = (int?)null,

                // ليس سائقًا
                LicenseNumber = (string?)null
            };

            var response =
                await _apiClient.PutAsync<ApiResponse<object>>(
                    $"Users/{id}",
                    payload);

            if (response == null || !response.Success)
            {
                _logger.LogWarning("فشل UpdateAsync للعميل {Id}. الرسالة: {Message}", id, response?.Message);
                throw new ApiServiceException(
                    HttpStatusCode.BadRequest,
                    ApiErrorCatalog.ClientUpdateFailed,
                    new object[] { id, response?.Message ?? "سبب غير معروف" });
            }

            return true;
        }
        catch (ApiServiceException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء في UpdateAsync للعميل {Id}", id);
            throw new ApiServiceException(HttpStatusCode.InternalServerError, ApiErrorCatalog.ServerError, innerException: ex);
        }
    }
}
