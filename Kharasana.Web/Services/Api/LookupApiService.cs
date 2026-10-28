using Microsoft.Extensions.Caching.Memory;
using Kharasana.Application.Common;
using Kharasana.Web.Services.Interfaces;
using Kharasana.Web.ViewModels.Shared;
using Kharasana.Web.ViewModels.ConcreteTypes;
using Kharasana.Web.ViewModels.Users;
using Kharasana.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kharasana.Web.Services.Api
{
    public class LookupApiService : ILookupApiService
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<LookupApiService> _logger;
        private readonly IMemoryCache _cache;

        public LookupApiService(ApiClient apiClient, ILogger<LookupApiService> logger, IMemoryCache cache)
        {
            _apiClient = apiClient;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>حجم الصفحة عند الجلب — يطابق سقف الـ API (PaginationParams يقصّ كل طلب إلى 100).</summary>
        private const int LookupPageSize = 100;

        /// <summary>سقف أمان لعدد الصفحات (2000 عنصر) حتى لا يتحول خلل في TotalCount إلى حلقة لا تنتهي.</summary>
        private const int MaxLookupPages = 20;

        /// <summary>
        /// جلب **كل** عناصر قائمة مرقّمة عبر صفحاتها المتتابعة.
        /// </summary>
        private async Task<List<TItem>> FetchPagedAsync<TItem, TResult>(
            string url,
            Func<IEnumerable<TResult>, IEnumerable<TItem>> extractor)
        {
            var items = new List<TItem>();
            var pageNumber = 1;
            var totalCount = int.MaxValue;
            var separator = url.Contains('?') ? '&' : '?';

            while (items.Count < totalCount && pageNumber <= MaxLookupPages)
            {
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<TResult>>>(
                    $"{url}{separator}PageNumber={pageNumber}&PageSize={LookupPageSize}");

                if (response == null || !response.Success || response.Data?.Items == null)
                {
                    _logger.LogWarning("لا بيانات في الصفحة {Page} من {Url}", pageNumber, url);
                    break;
                }

                items.AddRange(extractor(response.Data.Items));
                totalCount = response.Data.TotalCount;
                pageNumber++;
            }

            return items;
        }

        /// <summary>
        /// جلب قائمة اختيار (سائقون/عملاء) مع توحيد التسجيل.
        /// </summary>
        private async Task<List<LookupDto>> FetchUserLookupAsync(
            string operation,
            string url,
            Func<UserDto, LookupDto> project)
        {
            _logger.LogInformation("جلب {Operation}...", operation);

            var users = await FetchPagedAsync<UserDto, UserDto>(url, items => items);
            var items = users.Select(project).ToList();

            _logger.LogInformation("وُجد {Count} {Operation}", items.Count, operation);

            return items;
        }

        /// <summary>
        /// جلب جميع السائقين المتاحين في النظام
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversAsync()
        {
            return await FetchUserLookupAsync(
                "السائقون المتاحون",
                $"Users?role={(int)UserRole.Driver}&driverStatus={(int)DriverStatus.Available}&isActive=true",
                u => new LookupDto
                {
                    Id = u.UserId,
                    Name = u.FullName,
                    FactoryId = u.FactoryId
                });
        }

        /// <summary>
        /// جلب أنواع الخرسانة الخاصة بمصنع معين.
        /// </summary>
        public async Task<List<LookupDto>> GetConcreteTypesAsync(int factoryId)
        {
            string cacheKey = $"ConcreteTypes_Lookup_{factoryId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                try
                {
                    _logger.LogInformation("جلب أنواع الخرسانة للمصنع {FactoryId} من API...", factoryId);

                    var items = await FetchPagedAsync<LookupDto, ConcreteTypeDto>(
                        $"ConcreteTypes?factoryId={factoryId}",
                        items => items
                            .Where(x => x.IsActive)
                            .Select(x => new LookupDto
                            {
                                Id = x.ConcreteTypeId,
                                Name = x.Name,
                                UnitPrice = x.UnitPrice
                            }));

                    _logger.LogInformation("وُجد {Count} نوع خرسانة للمصنع {FactoryId}", items.Count, factoryId);
                    return items;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في جلب أنواع الخرسانة للمصنع {FactoryId}", factoryId);
                    throw;
                }
            }) ?? new List<LookupDto>();
        }
    }
}