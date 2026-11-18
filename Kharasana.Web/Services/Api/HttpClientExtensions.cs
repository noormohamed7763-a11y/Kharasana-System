using Kharasana.Application.Common;
using System.Net.Http;
using System.Text.Json;

namespace Kharasana.Web.Services.Api;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static async Task<T?> GetFromJsonSafeAsync<T>(this HttpClient httpClient, string url, CancellationToken cancellationToken = default)
    {
        // استخدام ApiClient للمناداة الآمنة أو تنفيذ طلب مباشر إذا كان ApiClient غير متاح
        // في هذا السياق، سننفذ GET بسيط ونحلل النتائج.
        // ملاحظة: الأفضل استخدام ApiClient.GetAsync<T> في المشاريع الكبيرة
        // ولكن للتوافق السريع مع الكود الحالي:
        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return default;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
