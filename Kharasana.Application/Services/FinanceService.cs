using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Application.Mapping;
using Kharasana.Domain.Common;
using Kharasana.Domain.Enums;

namespace Kharasana.Application.Services;

public class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public FinanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OrderPaymentDto>> GetPendingPaymentsAsync(int factoryId)
    {
        var payments = await _unitOfWork.OrderPayments.GetPendingVerificationsAsync(factoryId);
        return payments.Select(OrderMapper.MapToPaymentDto);
    }

    public async Task<bool> ApprovePaymentAsync(int paymentId, string verificationNotes, int approverId)
    {
        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        if (payment.VerificationStatus == PaymentVerificationStatus.NotApplicable)
            throw new BusinessException(Messages.PaymentCannotBeApprovedCash);

        payment.VerificationStatus = PaymentVerificationStatus.Verified;
        payment.VerificationNotes = verificationNotes;
        payment.VerifiedAt = DateTime.UtcNow;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectPaymentAsync(int paymentId, string rejectionReason, int approverId)
    {
        var payment = await _unitOfWork.OrderPayments.GetByIdIncludingDeletedAsync(paymentId);
        if (payment == null)
            throw new NotFoundException(Messages.PaymentNotFound);

        payment.VerificationStatus = PaymentVerificationStatus.Rejected;
        payment.VerificationNotes = rejectionReason;
        payment.VerifiedAt = DateTime.UtcNow;

        _unitOfWork.OrderPayments.Update(payment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
