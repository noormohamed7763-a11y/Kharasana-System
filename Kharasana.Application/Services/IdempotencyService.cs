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
            // إذا كانت StatusCode 0 = قيد التنفيذ، نتحقق من انتهاء الصلاحية
            if (existing.StatusCode == 0)
            {
                if (existing.ExpiresAt.HasValue && existing.ExpiresAt.Value < DateTime.UtcNow)
                {
                    // انتهت صلاحية العملية المعلقة - نسمح بإعادة المحاولة
                    return (false, default, 0);
                }
                
                // لا تزال قيد التنفيذ
                throw new ConflictException("العملية قيد التنفيذ حالياً، يرجى الانتظار.");
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
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10) // انتهاء صلاحية مؤقت للعمليات المعلقة
        };

        try
        {
            await _idempotencyRepository.AddAsync(newRecord);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // هذا يعني وجود race condition: سجل آخر تم إنشاؤه في هذه اللحظة
            // نعيد محاولة القراءة مرة واحدة
            return await CheckAndInitializeAsync<TResult>(key, userId, operationType, requestHash);
        }

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