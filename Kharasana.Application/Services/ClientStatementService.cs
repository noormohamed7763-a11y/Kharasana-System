using Kharasana.Application.Common;
using Kharasana.Application.Common.Exceptions;
using Kharasana.Application.DTOs.Client;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Application.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kharasana.Domain.Enums;


namespace Kharasana.Application.Services;

public class ClientStatementService : IClientStatementService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    public ClientStatementService(
        IOrderRepository orderRepository,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    public async Task<ClientStatementDto> GetClientStatementAsync(int clientId, CallerContext caller)
    {
        // 1. Validate Caller and Client Access
        if (caller.Role == UserRole.FactoryEmployee && caller.FactoryId == null)
        {
            throw new UnauthorizedException(Messages.CallerFactoryIdNotFound);
        }

        if (caller.Role == UserRole.Client && caller.UserId != clientId)
        {
            throw new ForbiddenException(Messages.ClientNotInCallerFactory);
        }

        var client = await _userRepository.GetByIdAsync(clientId);
        if (client == null || client.Role != UserRole.Client)
        {
            throw new NotFoundException(Messages.ClientNotFound);
        }

        var orders = new List<Order>();
        int pageNumber = 1;
        const int pageSize = 200; // reasonable default
        while (true)
        {
            var page = await _orderRepository.GetByClientIdWithPaymentsPagedAsync(
                clientId,
                caller.Role == UserRole.FactoryEmployee ? caller.FactoryId : null,
                pageNumber,
                pageSize);
            if (page == null || page.Count == 0)
                break;
            orders.AddRange(page);
            if (page.Count < pageSize)
                break; // last page
            pageNumber++;
        }

        // Factory employees can only view statements for clients in their factory.
        // The repository already filtered by factoryId, but keep a guard for safety.
        if (caller.Role == UserRole.FactoryEmployee && !orders.Any())
        {
            throw new ForbiddenException(Messages.ClientNotInCallerFactory);
        }

        var orderDtos = orders
            .Select(order => OrderMapper.MapToOrderDto(order, hidePricing: false))
            .ToList();

        // 5. Build Ledger Items
        var ledgerItems = new List<ClientStatementItemDto>();
        decimal runningBalance = 0;

        // Build ledger items iteratively to reduce memory usage
        // 1. Sort orders chronologically
        var sortedOrders = orders.OrderBy(o => o.CreatedAt).ToList();

        foreach (var order in sortedOrders)
        {
            // Debit entry for the order
            var debitItem = new ClientStatementItemDto
            {
                Date = order.CreatedAt,
                Reference = order.ProjectName,
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                Description = string.Format(Messages.OrderNumberStatement, order.OrderNumber),
                Debit = order.NetAmount
            };
            runningBalance += order.NetAmount;
            debitItem.RunningBalance = runningBalance;
            ledgerItems.Add(debitItem);

            // Credit entries for verified payments, ordered by payment date
            var verifiedPayments = order.Payments
                .Where(p => p.VerificationStatus == PaymentVerificationStatus.Verified)
                .OrderBy(p => p.CreatedAt);

            foreach (var payment in verifiedPayments)
            {
                var creditItem = new ClientStatementItemDto
                {
                    Date = payment.CreatedAt,
                    Reference = payment.ReferenceNumber,
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    Description = string.Format(Messages.PaymentStatement, payment.Amount,
                        PaymentMethodHelper.GetArabicName(payment.PaymentMethod)),
                    Credit = payment.Amount
                };
                runningBalance -= payment.Amount;
                creditItem.RunningBalance = runningBalance;
                ledgerItems.Add(creditItem);
            }
        }

        // 6. Calculate Summary
        var totalOrders = orders.Count;
        var totalQuantity = orders.Sum(o => o.Quantity);
        var totalAmount = orders.Sum(o => o.NetAmount);
        var totalPaid = orders.SelectMany(o => o.Payments).Sum(p => p.Amount);
        var balance = totalAmount - totalPaid;

        return new ClientStatementDto
        {
            ClientId = clientId,
            ClientName = client.FullName,
            Phone = client.Phone,
            StatementDate = DateTime.UtcNow,
            TotalOrders = totalOrders,
            TotalQuantity = totalQuantity,
            TotalAmount = totalAmount,
            TotalPaid = totalPaid,
            Balance = balance,
            LedgerItems = ledgerItems,
            Orders = orderDtos
        };
    }

    // Internal DTO for ledger events
    private class LedgerEvent
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public decimal Amount { get; set; }
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
    }
}
