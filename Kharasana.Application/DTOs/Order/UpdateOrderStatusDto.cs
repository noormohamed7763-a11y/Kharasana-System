using Kharasana.Domain.Enums;

namespace Kharasana.Application.DTOs.Order;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}