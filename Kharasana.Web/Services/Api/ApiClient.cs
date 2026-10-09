using Kharasana.Application.Common;
using Kharasana.Web.Configuration;
using Kharasana.Web.Localization;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kharasana.Web.Services.Api;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>نموذج مبسّط لقراءة Message من جسم خطأ الـ API دون ربط Web بهيكل Application.</summary>
    private sealed class ApiErrorEnvelope
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    public ApiClient(
        HttpClient httpClient,
        IOptions<ApiSettings> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _apiSettings = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_apiSettings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
        }
    }

    /// <summary>
    /// يعيد HttpClient الداخلي للاستخدام في البث المباشر للملفات (مثل FilesController).
    /// </summary>
    internal HttpClient HttpClient => _httpClient;

    /// <summary>
    /// معرّف الطلب الحالي — يُضمّن في السجلات لربط فشل واجهة برمجية
    /// بالرقم المرجعي الذي يراه المستخدم في صفحة الخطأ.
    /// </summary>
    private string? CurrentTraceId => _httpContextAccessor.HttpContext?.TraceIdentifier;

    /// <summary>
    /// يقرأ JWT من الجلسة الحالية. يُستخدم لضبط Authorization على
    /// HttpRequestMessage الفردي بدلاً من DefaultRequestHeaders المشترك —
    /// مما يمنع تداخل الرموز بين الجلسات المتزامنة (race condition).
    /// </summary>
    private string? GetToken()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // ✅ التخزين المؤقت في Items لكل طلب لتقليل الوصول المتكرر للجلسة
        if (context.Items.TryGetValue("ApiToken", out var cachedToken))
            return cachedToken as string;

        try
        {
            var token = context.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
                context.Items["ApiToken"] = token;

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "تعذّرت قراءة رمز الجلسة. التتبّع={TraceId}", CurrentTraceId);
            return null;
        }
    }

    /// <summary>
    /// يحوّل حالة HTTP إلى نموذج خطأ مناسب، مع تفضيل رسالة الـ API الأصلية
    /// عندما يعيد الخادم ApiResponse بجسم قابل للقراءة.
    /// </summary>
    private static ApiError ComposeApiError(HttpStatusCode statusCode, string? apiMessage)
    {
        var error = statusCode switch
        {
            HttpStatusCode.Unauthorized => ApiErrorCatalog.Unauthorized,
            HttpStatusCode.Forbidden => ApiErrorCatalog.Forbidden,
            HttpStatusCode.NotFound => ApiErrorCatalog.NotFound,
            _ when (int)statusCode >= 500 => ApiErrorCatalog.ServerError,
            _ => ApiErrorCatalog.Unknown
        };

        if (!string.IsNullOrWhiteSpace(apiMessage))
        {
            // نستخدم رسالة الـ API الأصلية إذا توفرت ولكن نحتفظ بنفس الكود
            return error with { UserMessageTemplate = apiMessage };
        }

        return error;
    }

    private async Task<T?> HandleResponseAsync<T>(
        HttpResponseMessage response,
        string method,
        string url)
    {
        var content = await response.Content.ReadAsStringAsync();

        // ✅ تسجيل معلومات الاستجابة بدون المحتوى
        _logger.LogInformation(
            "الـ API {Method} {Url} أعاد الحالة: {StatusCode}",
            method,
            url,
            (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "الـ API {Method} {Url} أعاد حالة غير ناجحة: {StatusCode}. التتبّع={TraceId}",
                method,
                url,
                (int)response.StatusCode,
                CurrentTraceId);

            string? apiMessage = null;

            // محاولة استخراج رسالة الخطأ العربية من جسم الـ API (ApiResponse)
            if (!string.IsNullOrWhiteSpace(content))
            {
                // تسجيل محتوى الخطأ (مقتطعاً) للمساعدة في التشخيص
                var truncated = content.Length > 500 ? content[..500] + "..." : content;
                _logger.LogWarning(
                    "جسم خطأ الـ API {Method} {Url}: {Body}. التتبّع={TraceId}",
                    method,
                    url,
                    truncated,
                    CurrentTraceId);

                try
                {
                    var envelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(content, JsonOptions);
                    apiMessage = string.IsNullOrWhiteSpace(envelope?.Message) ? null : envelope.Message;
                }
                catch
                {
                    // الجسم ليس ApiResponse (مثلاً 401 بلا جسم من مستوى Framework) — يبقى null ونلجأ للرسالة العامة
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("غير مُصرَّح. التتبّع={TraceId}", CurrentTraceId);
            }

            var error = ComposeApiError(response.StatusCode, apiMessage);

            throw new ApiServiceException(
                response.StatusCode,
                error,
                traceId: CurrentTraceId);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "تعذّر تحليل ردّ ناجح لـ {Method} {Url}. التتبّع={TraceId}", method, url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.OK,
                ApiErrorCatalog.Unknown,
                traceId: CurrentTraceId,
                innerException: ex);
        }
    }

    /// <summary>
    /// يعيد إجمالي عدد العناصر (TotalCount) لقائمة GET دون جلب بيانات الصفحة —
    /// يُستخدم في بطاقات الإحصاءات لتغذيتها بالأرقام الحقيقية عبر كل الصفحات.
    /// </summary>
    public async Task<int> GetPagedTotalAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<ApiResponse<PagedResult<T>>>(url, cancellationToken);
        return response?.Success == true && response.Data != null
            ? response.Data.TotalCount
            : 0;
    }

    /// <summary>
    /// يلتقط أخطاء النقل (انقطاع الشبكة / انتهاء المهلة) ولا يُقيّد مكالمات get/post
    /// خارج نطاقها — يعيد استثناء مستخدم عربياً بدلاً من أن ينفلت الاستثناء التقني للمستخدم.
    /// </summary>
    private void ThrowTransportException(string method, string url, Exception ex)
    {
        var error = ex switch
        {
            HttpRequestException => ApiErrorCatalog.NetworkError,
            _ => ApiErrorCatalog.ServerError
        };

        var statusCode = ex switch
        {
            HttpRequestException => HttpStatusCode.BadGateway,
            _ => HttpStatusCode.InternalServerError
        };

        throw new ApiServiceException(statusCode, error, traceId: CurrentTraceId, innerException: ex);
    }

    /// <summary>
    /// ينفّذ طلب HTTP عبر المفوض send ويوحّد معالجة الأخطاء لكل الأفعال
    /// (GET/POST/PUT/DELETE/ملفات): يعيد طرح ApiServiceException كما هي،
    /// ويحوّل انتهاء المهلة والإلغاء وأخطاء النقل إلى رسائل مستخدم عربية.
    /// يُضاف هنا Authorization على الطلب الفردي — ليس على DefaultRequestHeaders.
    /// </summary>
    private async Task<T?> SendAsync<T>(
        string method,
        string url,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return await HandleResponseAsync<T>(response, method, url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw; // إلغاء خارجي (غادر المستخدم الصفحة) — لا حاجة لرسالة

            _logger.LogWarning("انتهت مهلة {Method} {Url}. التتبّع={TraceId}", method, url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                AppMessages.Common.RequestTimeout,
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثناء أثناء {Method} {Url}. التتبّع={TraceId}", method, url, CurrentTraceId);
            ThrowTransportException(method, url, ex);
            return default;
        }
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        return await SendAsync<T>(
            "GET",
            url,
            request,
            cancellationToken);
    }

    public async Task<T?> PostAsync<T>(string url, object data, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);

        // ✅ تسجيل معلومات الطلب بدون المحتوى
        _logger.LogInformation("إرسال طلب POST إلى {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await SendAsync<T>(
            "POST",
            url,
            request,
            cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string url, object data, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(data);

        // ✅ تسجيل معلومات الطلب بدون المحتوى
        _logger.LogInformation("إرسال طلب PUT إلى {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await SendAsync<T>(
            "PUT",
            url,
            request,
            cancellationToken);
    }

    /// <summary>
    /// PUT بدون جسم (body) — للعمليات مثل toggle-active
    /// </summary>
    public async Task<T?> PutAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("إرسال طلب PUT إلى {Url} (بلا جسم)", url);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent("{ }", Encoding.UTF8, "application/json")
        };

        return await SendAsync<T>(
            "PUT",
            url,
            request,
            cancellationToken);
    }

    public async Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);

        return await SendAsync<T>(
            "DELETE",
            url,
            request,
            cancellationToken);
    }

    public async Task<T?> PostFileAsync<T>(string url, Stream fileStream, string fileName, string parameterName = "file", CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, parameterName, fileName);

        _logger.LogInformation("إرسال طلب POST متعدّد الأجزاء إلى {Url} بالملف {FileName}", url, fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        return await SendAsync<T>(
            "POST-FILE",
            url,
            request,
            cancellationToken);
    }
}