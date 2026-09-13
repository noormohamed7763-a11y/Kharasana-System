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
    /// معرّف الطلب الحالي — يُضمَّن في السجلات لربط فشل واجهة برمجية
    /// بالرقم المرجعي الذي يراه المستخدم في صفحة الخطأ.
    /// </summary>
    private string? CurrentTraceId => _httpContextAccessor.HttpContext?.TraceIdentifier;

    private void AddAuthorizationHeader()
    {
        try
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Authorization header. TraceId={TraceId}", CurrentTraceId);
        }
    }

    /// <summary>
    /// يحوّل حالة HTTP إلى رسالة عربية مناسبة، مع تفضيل رسالة الـ API الأصلية
    /// عندما يعيد الخادم ApiResponse بجسم قابل للقراءة.
    /// </summary>
    private static string ComposeUserMessage(HttpStatusCode statusCode, string? apiMessage)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage))
            return apiMessage;

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => AppMessages.Common.Unauthorized,
            HttpStatusCode.Forbidden => AppMessages.Common.Forbidden,
            HttpStatusCode.NotFound => AppMessages.Common.NotFound,
            HttpStatusCode.Conflict => "البيانات مستخدمة مسبقاً أو متعارضة.",
            HttpStatusCode.TooManyRequests => "طلبات كثيرة من جهازك. انتظر قليلاً ثم أعد المحاولة.",
            _ when (int)statusCode >= 500 => AppMessages.Common.ServerError,
            _ => AppMessages.Common.OperationFailed
        };
    }

    private async Task<T?> HandleResponseAsync<T>(
        HttpResponseMessage response,
        string method,
        string url)
    {
        var content = await response.Content.ReadAsStringAsync();

        // ✅ تسجيل معلومات الاستجابة بدون المحتوى
        _logger.LogInformation(
            "API {Method} {Url} returned StatusCode: {StatusCode}",
            method,
            url,
            (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "API {Method} {Url} returned non-success StatusCode: {StatusCode}. TraceId={TraceId}",
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
                    "API {Method} {Url} error body: {Body}. TraceId={TraceId}",
                    method,
                    url,
                    truncated,
                    CurrentTraceId);

                try
                {
                    var error = JsonSerializer.Deserialize<ApiErrorEnvelope>(content, JsonOptions);
                    apiMessage = string.IsNullOrWhiteSpace(error?.Message) ? null : error.Message;
                }
                catch
                {
                    // الجسم ليس ApiResponse (مثلاً 401 بلا جسم من مستوى Framework) — يبقى null ونلجأ للرسالة العامة
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("Unauthorized. Clearing session. TraceId={TraceId}", CurrentTraceId);
                _httpContextAccessor.HttpContext?.Session.Clear();
            }

            var userMessage = ComposeUserMessage(response.StatusCode, apiMessage);

            throw new ApiServiceException(
                response.StatusCode,
                userMessage,
                apiMessage,
                CurrentTraceId);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to deserialize successful response for {Method} {Url}. TraceId={TraceId}", method, url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.OK,
                AppMessages.Common.ServerError,
                apiResponseMessage: null,
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
        var userMessage = ex switch
        {
            HttpRequestException => AppMessages.Common.NetworkError,
            _ => AppMessages.Common.ServerError
        };

        var statusCode = ex switch
        {
            HttpRequestException => HttpStatusCode.BadGateway,
            _ => HttpStatusCode.InternalServerError
        };

        throw new ApiServiceException(statusCode, userMessage, traceId: CurrentTraceId, innerException: ex);
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            return await HandleResponseAsync<T>(response, "GET", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw; // إلغاء خارجي (غادر المستخدم الصفحة) — لا حاجة لرسالة

            _logger.LogWarning("GET {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while GET {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("GET", url, ex);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string url, object data, CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        var json = JsonSerializer.Serialize(data);

        // ✅ تسجيل معلومات الطلب بدون المحتوى
        _logger.LogInformation("📤 Sending POST request to {Url}", url);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return await HandleResponseAsync<T>(response, "POST", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogWarning("POST {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while POST {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("POST", url, ex);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string url, object data, CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        var json = JsonSerializer.Serialize(data);

        // ✅ تسجيل معلومات الطلب بدون المحتوى
        _logger.LogInformation("📤 Sending PUT request to {Url}", url);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PutAsync(url, content, cancellationToken);
            return await HandleResponseAsync<T>(response, "PUT", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogWarning("PUT {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while PUT {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("PUT", url, ex);
            return default;
        }
    }

    /// <summary>
    /// PUT بدون جسم (body) — للعمليات مثل toggle-active
    /// </summary>
    public async Task<T?> PutAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        _logger.LogInformation("📤 Sending PUT request to {Url} (no body)", url);

        using var content = new StringContent("{ }", Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.PutAsync(url, content, cancellationToken);
            return await HandleResponseAsync<T>(response, "PUT", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogWarning("PUT {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while PUT {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("PUT", url, ex);
            return default;
        }
    }

    public async Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        try
        {
            using var response = await _httpClient.DeleteAsync(url, cancellationToken);
            return await HandleResponseAsync<T>(response, "DELETE", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogWarning("DELETE {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while DELETE {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("DELETE", url, ex);
            return default;
        }
    }

    public async Task<T?> PostFileAsync<T>(string url, Stream fileStream, string fileName, string parameterName = "file", CancellationToken cancellationToken = default)
    {
        AddAuthorizationHeader();

        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, parameterName, fileName);

            _logger.LogInformation("📤 Sending Multipart POST to {Url} with file {FileName}", url, fileName);

            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return await HandleResponseAsync<T>(response, "POST-FILE", url);
        }
        catch (ApiServiceException) { throw; }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;

            _logger.LogWarning("Multipart POST {Url} timed out. TraceId={TraceId}", url, CurrentTraceId);
            throw new ApiServiceException(
                HttpStatusCode.GatewayTimeout,
                "استغرق الاتصال بالنظام وقتاً طويلاً. حاول مرة أخرى.",
                traceId: CurrentTraceId,
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while Multipart POST {Url}. TraceId={TraceId}", url, CurrentTraceId);
            ThrowTransportException("POST-FILE", url, ex);
            return default;
        }
    }
}