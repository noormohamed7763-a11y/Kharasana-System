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

        /// <summary>حجم الصفحة عند الجلب — يطابق سقف الـ API (PaginationParams يقصّ كل طلب إلى 100).</summary>
        private const int LookupPageSize = 100;

        /// <summary>سقف أمان لعدد الصفحات (2000 عنصر) حتى لا يتحول خلل في TotalCount إلى حلقة لا تنتهي.</summary>
        private const int MaxLookupPages = 20;

        /// <summary>
        /// يجلب **كل** عناصر قائمة مرقّمة عبر صفحاتها المتتابعة.
        ///
        /// <para>الـ API يقيّد PageSize بسقف 100، فطلب صفحة واحدة كان يُسقط كل ما بعد
        /// العنصر المئة صامتاً — فيغيب سائق متاح أو عميل موجود عن القائمة المنسدلة
        /// بلا أي إشارة ويستحيل اختياره. هنا نقرأ TotalCount من الصفحة الأولى ثم نكمل
        /// ما بقي.</para>
        /// </summary>
        private async Task<List<UserDto>> FetchAllUsersAsync(string url)
        {
            var items = new List<UserDto>();
            var pageNumber = 1;
            var totalCount = int.MaxValue;
            var separator = url.Contains('?') ? '&' : '?';

            while (items.Count < totalCount && pageNumber <= MaxLookupPages)
            {
                var response = await _apiClient.GetAsync<ApiResponse<PagedResult<UserDto>>>(
                    $"{url}{separator}PageNumber={pageNumber}&PageSize={LookupPageSize}");

                if (response == null || !response.Success || response.Data?.Items == null)
                {
                    _logger.LogWarning("No lookup data on page {Page} of {Url}", pageNumber, url);
                    break;
                }

                items.AddRange(response.Data.Items);
                totalCount = response.Data.TotalCount;
                pageNumber++;
            }

            if (items.Count < totalCount)
            {
                _logger.LogWarning(
                    "Lookup truncated: fetched {Fetched} of {Total} from {Url}",
                    items.Count,
                    totalCount,
                    url);
            }

            return items;
        }

        /// <summary>
        /// جلب قائمة اختيار (سائقون/عملاء) مع توحيد التسجيل.
        ///
        /// <para>⚠️ لا يُبتلع <see cref="ApiServiceException"/> هنا: تعذّر الوصول إلى الـ API
        /// كان يُعيد قائمة فارغة، فتظهر المنسدلة وكأنه «لا يوجد سائقون متاحون» — نتيجة
        /// خاطئة تدفع المستخدم لقرار خاطئ (يظن أن لا سائقين بينما الخدمة متوقفة).
        /// يُطرح الاستثناء ليعرضه المتحكّم برسالة صريحة؛ وكل مواضع الاستدعاء تُحيط
        /// النداء بـ try/catch يعالج ApiServiceException.</para>
        /// </summary>
        private async Task<List<LookupDto>> FetchUserLookupAsync(
            string operation,
            string url,
            Func<UserDto, LookupDto> project)
        {
            _logger.LogInformation("Fetching {Operation}...", operation);

            var users = await FetchAllUsersAsync(url);
            var items = users.Select(project).ToList();

            _logger.LogInformation("Found {Count} {Operation}", items.Count, operation);

            return items;
        }

        /// <summary>
        /// جلب جميع السائقين المتاحين في النظام
        ///
        /// <para>التصفية على الخادم (<c>isActive=true</c>): كانت تُجلب الصفحة الأولى ثم
        /// تُصفّى في الذاكرة، فيسقط سائق نشط وقع خارج تلك الصفحة.</para>
        /// </summary>
        public async Task<List<LookupDto>> GetAvailableDriversAsync()
        {
            return await FetchUserLookupAsync(
                "available drivers",
                $"Users?role={(int)UserRole.Driver}&driverStatus={(int)DriverStatus.Available}&isActive=true",
                u => new LookupDto
                {
                    Id = u.UserId,
                    Name = u.FullName,
                    FactoryId = u.FactoryId
                });
        }

        /// <summary>
        /// جلب جميع العملاء
        ///
        /// <para>التصفية على الخادم (<c>isActive=true</c>) بدل تصفية صفحة واحدة في الذاكرة.</para>
        /// </summary>
        public async Task<List<LookupDto>> GetClientsAsync()
        {
            return await FetchUserLookupAsync(
                "clients",
                $"Users?role={(int)UserRole.Client}&isActive=true",
                u => new LookupDto
                {
                    Id = u.UserId,
                    Name = u.FullName,
                    FactoryId = u.FactoryId
                });
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