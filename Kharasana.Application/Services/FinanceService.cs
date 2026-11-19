using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Mapping;
using Kharasana.Domain.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kharasana.Application.Services;

public class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdempotencyService _idempotencyService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public FinanceService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IIdempotencyService idempotencyService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _idempotencyService = idempotencyService;
    }

    public async Task<IEnumerable<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId, int pageNumber = 1, int pageSize = 10)
    {
        // التحقق من الصلاحيات: Admin أو الموظف التابع للمصنع نفسه
        if (!_currentUserService.IsInRole("Admin") && _currentUserService.FactoryId != factoryId)
            throw new UnauthorizedException("ليس لديك صلاحية الوصول لبيانات هذا المصنع.");

        var payments = await _unitOfWork.OrderPayments.GetPendingVerificationsAsync(factoryId, pageNumber, pageSize);
        return payments.Select(p => new OrderPaymentDto
        {
            Id = p.OrderPaymentId,
            OrderId = p.OrderId,
            Amount = p.Amount,
            PaymentDate = p.PaymentDate,
            PaymentMethod = p.PaymentMethod,
            ReferenceNo = p.ReferenceNumber,
            Notes = p.VerificationNotes,
            VerificationStatus = p.VerificationStatus,
            VerificationNotes = p.VerificationNotes,
            VerifiedAt = p.VerifiedAt
        });
    }

    public async Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes, string? idempotencyKey = null)
    {
        var currentUserId = int.Parse(_currentUserService.UserId ?? "0");

        // ✅ Idempotency Check
        string? requestHash = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            requestHash = ComputeRequestHash(paymentId, verificationNotes, currentUserId, "ApprovePayment");

            var (isDuplicate, existingResult, _) = await _idempotencyService
                .CheckAndInitializeAsync<bool>(idempotencyKey, currentUserId, "ApprovePayment", requestHash);

            if (isDuplicate) return existingResult;
        }

        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        // التحقق من الصلاحيات: Admin أو الموظف التابع للمصنع نفسه
        if (!_currentUserService.IsInRole("Admin") && payment.Order.FactoryId != _currentUserService.FactoryId)
            throw new UnauthorizedException("ليس لديك صلاحية لاعتماد دفعات لهذا المصنع.");

        if (payment.VerificationStatus == PaymentVerificationStatus.Verified)
            return true; // مسموح بالإعادة إذا كانت الحالة هي المطلوبة بالفعل

        if (payment.VerificationStatus == PaymentVerificationStatus.NotApplicable)
            throw new BusinessException(Messages.PaymentCannotBeApprovedCash);

        payment.VerificationStatus = PaymentVerificationStatus.Verified;
        payment.VerificationNotes = verificationNotes;
        payment.VerifiedAt = DateTime.UtcNow;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        // ✅ تسجيل نجاح العملية في Idempotency
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && requestHash != null)
        {
            await _idempotencyService.CompleteAsync(idempotencyKey, currentUserId, "ApprovePayment", true, 200);
        }

        return true;
    }

    public async Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason, string? idempotencyKey = null)
    {
        var currentUserId = int.Parse(_currentUserService.UserId ?? "0");

        // ✅ Idempotency Check
        string? requestHash = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            requestHash = ComputeRequestHash(paymentId, rejectionReason, currentUserId, "RejectPayment");

            var (isDuplicate, existingResult, _) = await _idempotencyService
                .CheckAndInitializeAsync<bool>(idempotencyKey, currentUserId, "RejectPayment", requestHash);

            if (isDuplicate) return existingResult;
        }

        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        // التحقق من الصلاحيات: Admin أو الموظف التابع للمصنع نفسه
        if (!_currentUserService.IsInRole("Admin") && payment.Order.FactoryId != _currentUserService.FactoryId)
            throw new UnauthorizedException("ليس لديك صلاحية لرفض دفعات لهذا المصنع.");

        if (payment.VerificationStatus == PaymentVerificationStatus.Rejected)
            return true;

        if (payment.VerificationStatus == PaymentVerificationStatus.NotApplicable)
            throw new BusinessException(Messages.PaymentCannotBeRejectedCash);

        payment.VerificationStatus = PaymentVerificationStatus.Rejected;
        payment.VerificationNotes = rejectionReason;
        payment.VerifiedAt = DateTime.UtcNow;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        // ✅ تسجيل نجاح العملية في Idempotency
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && requestHash != null)
        {
            await _idempotencyService.CompleteAsync(idempotencyKey, currentUserId, "RejectPayment", true, 200);
        }

        return true;
    }

    private string ComputeRequestHash(int paymentId, string notes, int userId, string operation)
    {
        var rawData = $"{operation}:{paymentId}:{notes}:{userId}";
        var bytes = Encoding.UTF8.GetBytes(rawData);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
