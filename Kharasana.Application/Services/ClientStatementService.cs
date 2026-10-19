using AutoMapper;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Client;
using Kharasana.Application.Interfaces.Repositories;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kharasana.Application.Services;

public class ClientStatementService : IClientStatementService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ClientStatementService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ClientStatementDto> GetClientStatementAsync(int clientId, CallerContext caller)
    {
        // 1. Validate Caller and Client Access
        if (caller.Role == UserRole.FactoryEmployee && caller.FactoryId == null)
        {
            throw new ApplicationException(Messages.CallerFactoryIdNotFound);
        }

        var client = await _userRepository.GetByIdAsync(clientId);
        if (client == null || client.Role != UserRole.Client)
        {
            throw new ApplicationException(Messages.ClientNotFound);
        }

        // Factory employees can only view statements for clients in their factory
        if (caller.Role == UserRole.FactoryEmployee)
        {
            // Need to check if the client has orders in the caller's factory
            // This check can be implicitly handled by the order query, but an explicit check might be clearer
            var clientOrders = await _orderRepository.GetByClientIdWithPaymentsAsync(clientId);
            if (!clientOrders.Any(o => o.FactoryId == caller.FactoryId))
            {
                throw new ApplicationException(Messages.ClientNotInCallerFactory);
            }
        }

        // 2. Fetch Orders for the client
        var orders = await _orderRepository.GetByClientIdWithPaymentsAsync(clientId);

        // 3. Filter orders if caller is FactoryEmployee
        if (caller.Role == UserRole.FactoryEmployee)
        {
            orders = orders.Where(o => o.FactoryId == caller.FactoryId).ToList();
        }

        // 4. Map Orders to OrderDto (for full order details if needed in the UI)
        var orderDtos = _mapper.Map<IReadOnlyList<DTOs.Order.OrderDto>>(orders);

        // 5. Build Ledger Items
        var ledgerItems = new List<ClientStatementItemDto>();
        decimal runningBalance = 0;

        // Sort all financial events chronologically
        var allEvents = orders
            .SelectMany(o =>
            {
                var events = new List<LedgerEvent>();

                // Order creation (Debit)
                events.Add(new LedgerEvent
                {
                    Date = o.CreatedAt,
                    Type = "Order",
                    Description = string.Format(Messages.OrderNumberStatement, o.OrderNumber),
                    Reference = o.ProjectName,
                    Amount = o.NetAmount,
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber
                });

                // Payments (Credit)
                events.AddRange(o.Payments.Select(p => new LedgerEvent
                {
                    Date = p.CreatedAt,
                    Type = "Payment",
                    Description = string.Format(Messages.PaymentStatement, p.Amount, PaymentMethodHelper.GetArabicName(p.PaymentMethod)),
                    Reference = p.ReferenceNumber,
                    Amount = p.Amount,
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber
                }));

                return events;
            })
            .OrderBy(e => e.Date)
            .ToList();

        foreach (var evt in allEvents)
        {
            var item = new ClientStatementItemDto
            {
                Date = evt.Date,
                Reference = evt.Reference,
                OrderId = evt.OrderId,
                OrderNumber = evt.OrderNumber
            };

            if (evt.Type == "Order")
            {
                item.Description = evt.Description;
                item.Debit = evt.Amount;
                runningBalance += evt.Amount;
            }
            else if (evt.Type == "Payment")
            {
                item.Description = evt.Description;
                item.Credit = evt.Amount;
                runningBalance -= evt.Amount;
            }

            item.RunningBalance = runningBalance;
            ledgerItems.Add(item);
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
