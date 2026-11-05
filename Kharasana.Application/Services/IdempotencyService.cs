using System.Text.Json;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;

namespace Kharasana.Application.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyRecordRepository _idempotencyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IdempotencyService(IIdempotencyRecordRepository idempotencyRepository, IUnitOfWork unitOfWork)
    {
        _idempotencyRepository = idempotencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool IsDuplicate, TResult? Result, int StatusCode)> CheckAndInitializeAsync<TResult>(
        string key,
        int userId,
        string operationType,
        string requestHash)
    {
        if (string.IsNullOrWhiteSpace(key))
            return (false, default, 0);

        var existing = await _idempotencyRepository.GetByKeyAndUserIdAsync(key, userId);

        if (existing != null)
        {
            // ✅ تحقق من تطابق البيانات
            if (existing.RequestHash != requestHash)
            {
                throw new ConflictException(
                    $"مفتاح Idempotency '{key}' مستخدم مع بيانات مختلفة للعملية '{operationType}'.");
            }

            // ✅ إذا كانت العملية مكتملة بنجاح سابقاً (StatusCode 2xx)
            if (existing.StatusCode >= 200 && existing.StatusCode < 300)
            {
                var result = existing.ResultPayload != null
                    ? JsonSerializer.Deserialize<TResult>(existing.ResultPayload, JsonOptions)
                    : default;

                return (true, result, existing.StatusCode);
            }

            // ✅ إذا كانت العملية السابقة فاشلة (4xx, 5xx) أو قيد التنفيذ (StatusCode=0)
            // نسمح بإعادة المحاولة بشرط أن تكون السياسة تسمح بذلك.
            // هنا نعتبر StatusCode 0 = قيد التنفيذ (لم يكتمل بعد).
            if (existing.StatusCode == 0)
            {
                // يمكن إضافة منطق: إذا كانت العملية قيد التنفيذ منذ وقت طويل، اعتبرها مهجورة
                // لكن حالياً نعيد false للسماح بإعادة المحاولة
                return (false, default, 0);
            }

            // عمليات فاشلة سابقاً (400, 500) - نسمح بالإعادة
            return (false, default, 0);
        }

        // ✅ لا يوجد سجل سابق - ننشئ سجل "قيد التنفيذ"
        var newRecord = new IdempotencyRecord
        {
            Key = key,
            UserId = userId,
            OperationType = operationType,
            RequestHash = requestHash,
            StatusCode = 0, // قيد التنفيذ
            CreatedAt = DateTime.UtcNow
        };

        await _idempotencyRepository.AddAsync(newRecord);
        await _unitOfWork.SaveChangesAsync();

        return (false, default, 0);
    }

    public async Task CompleteAsync<TResult>(
        string key,
        int userId,
        string operationType,
        TResult result,
        int statusCode)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var record = await _idempotencyRepository.GetByKeyAndUserIdAsync(key, userId);
        if (record == null)
        {
            // قد يحدث إذا لم يتم استدعاء CheckAndInitializeAsync أولاً
            record = new IdempotencyRecord
            {
                Key = key,
                UserId = userId,
                OperationType = operationType,
                RequestHash = string.Empty, // لا نملك الـ hash هنا
                StatusCode = statusCode,
                ResultEntityId = result?.ToString(),
                ResultPayload = JsonSerializer.Serialize(result, JsonOptions),
                CreatedAt = DateTime.UtcNow
            };
            await _idempotencyRepository.AddAsync(record);
        }
        else
        {
            record.StatusCode = statusCode;
            record.ResultEntityId = result?.ToString();
            record.ResultPayload = JsonSerializer.Serialize(result, JsonOptions);
            _idempotencyRepository.Update(record);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task FailAsync(
        string key,
        int userId,
        string operationType,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var record = await _idempotencyRepository.GetByKeyAndUserIdAsync(key, userId);
        if (record != null)
        {
            // نسجل الفشل بـ StatusCode = 500 أو نتركه 0 للسماح بإعادة المحاولة
            // السياسة: تركه 0 يسمح بإعادة المحاولة، وضع 500 يثبّت الفشل
            record.StatusCode = 500;
            record.ResultPayload = JsonSerializer.Serialize(new { Error = errorMessage });
            _idempotencyRepository.Update(record);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}