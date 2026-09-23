using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Shared;
using Kharasana.Application.DTOs.ConcreteType;
using Kharasana.Application.DTOs.User;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    public class LookupApiService : ILookupApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<LookupApiService> _logger;

        public LookupApiService(ApiClient apiClient, ILogger<LookupApiService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// تنفيذ طلب API موحّد مع معالجة الأخطاء والتسجيل.
        /// </summary>
        private async Task<(bool Success, List<LookupDto> Items)> FetchLookupAsync(
            string operation,
            string url,
            Func<ApiResponse<PagedResult<UserDto>>, List<LookupDto>> mapper)
        {
            try
            {
                _logger.LogInformation("Fetching {Operation}...", operation);

                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(url);

                if (response != null && response.Success && response.Data?.Items != null)
                {
                    var items = mapper(response);
                    _logger.LogInformation("Found {Count} {Operation}", items.Count, operation);
                    return (true, items);
                }

                _logger.LogWarning("No {Operation} found.", operation);
            }
            catch (ApiServiceException ex)
            {
                _logger.LogError(ex, "API error in {Operation}", operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching {Operation}", operation);
            }

            return (false, new List<LookupDto>());
        }

        /// <summary>
        /// جلب جميع السائقين المتاحين في النظام
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversAsync()
        {
            var (Success, Items) = await FetchLookupAsync(
                "available drivers",
                $"Users?role={(int)UserRole.Driver}&driverStatus={(int)DriverStatus.Available}&PageSize=100",
                response => response.Data!.Items
                    .Where(u => u.IsActive)
                    .Select(u => new LookupDto
                    {
                        Id = u.UserId,
                        Name = u.FullName,
                        FactoryId = u.FactoryId
                    })
                    .ToList()
            );

            return Success ? Items : new List<LookupDto>();
        }

        /// <summary>
        /// جلب السائقين المتاحين في مصنع معين فقط
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversByFactoryAsync(int factoryId)
        {
            var (Success, Items) = await FetchLookupAsync(
                $"available drivers for factory {factoryId}",
                $"Users?role={(int)UserRole.Driver}&driverStatus={(int)DriverStatus.Available}&factoryId={factoryId}&PageSize=100",
                response => response.Data!.Items
                    .Where(u => u.IsActive && u.FactoryId == factoryId)
                    .Select(u => new LookupDto
                    {
                        Id = u.UserId,
                        Name = u.FullName,
                        FactoryId = u.FactoryId
                    })
                    .ToList()
            );

            return Success ? Items : new List<LookupDto>();
        }

        /// <summary>
        /// جلب جميع العملاء
        /// </summary>
        public async Task<List<LookupDto>> GetClientsAsync()
        {
            var (Success, Items) = await FetchLookupAsync(
                "clients",
                $"Users?role={(int)UserRole.Client}&PageSize=100",
                response => response.Data!.Items
                    .Where(u => u.IsActive)
                    .Select(u => new LookupDto
                    {
                        Id = u.UserId,
                        Name = u.FullName,
                        FactoryId = u.FactoryId
                    })
                    .ToList()
            );

            return Success ? Items : new List<LookupDto>();
        }

        /// <summary>
        /// جلب جميع السائقين (بغض النظر عن حالتهم)
        /// </summary>
        public async Task<List<LookupDto>> GetDriversAsync()
        {
            var (Success, Items) = await FetchLookupAsync(
                "drivers",
                $"Users?role={(int)UserRole.Driver}&PageSize=100",
                response => response.Data!.Items
                    .Where(u => u.IsActive)
                    .Select(u => new LookupDto
                    {
                        Id = u.UserId,
                        Name = u.FullName,
                        FactoryId = u.FactoryId
                    })
                    .ToList()
            );

            return Success ? Items : new List<LookupDto>();
        }

        /// <summary>
        /// جلب أنواع الخرسانة
        /// </summary>
        public async Task<List<LookupDto>> GetConcreteTypesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching concrete types...");

                var response = await _apiClient.GetAsync<ApiResponse<List<ConcreteTypeDto>>>("ConcreteTypes");

                if (response != null && response.Success && response.Data != null)
                {
                    var types = response.Data
                        .Where(x => x.IsActive)
                        .Select(x => new LookupDto
                        {
                            Id = x.ConcreteTypeId,
                            Name = x.Name,
                            UnitPrice = x.UnitPrice
                        })
                        .ToList();

                    _logger.LogInformation("Found {Count} concrete types", types.Count);
                    return types;
                }

                _logger.LogWarning("No concrete types found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching concrete types");
            }

            return new List<LookupDto>();
        }
    }
}