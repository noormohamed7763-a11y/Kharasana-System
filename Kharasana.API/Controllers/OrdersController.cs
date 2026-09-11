using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.Order;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kharasana.Application.DTOs.Customer;

namespace Kharasana.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // ------------------------------------------------------------
    // Helper مشترك: استخراج هوية المستدعي
    // ------------------------------------------------------------
    private bool TryGetCallerContext(out int callerId, out UserRole callerRole, out int? callerFactoryId, out IActionResult? error)
    {
        callerId = 0;
        callerRole = default;
        callerFactoryId = null;
        error = null;

        if (!User.TryGetRole(out callerRole))
        {
            error = Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
            return false;
        }

        var uid = User.GetUserId();
        if (uid == null)
        {
            error = Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserIdNotFound, Data = null });
            return false;
        }

        callerId = uid.Value;
        callerFactoryId = User.GetFactoryId();
        return true;
    }

    // ============================================================
    // 1. GET ALL
    // ============================================================
    [HttpGet]
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        int? factoryId = null, clientId = null, driverId = null;

        switch (callerRole)
        {
            case UserRole.FactoryEmployee:
                if (callerFactoryId == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
                }
                factoryId = callerFactoryId;
                break;

            case UserRole.Client:
                clientId = callerId;
                break;

            case UserRole.Driver:
                driverId = callerId;
                break;

            case UserRole.Admin:
                // المدير يرى كل الطلبات، ويمكنه تضييق النطاق عبر فلتر المصنع في القائمة
                factoryId = pagination.FactoryId;
                break;
        }

        var result = await _orderService.GetPagedAsync(factoryId, clientId, driverId, callerRole, pagination);

        return Ok(new ApiResponse<PagedResult<OrderDto>>
        {
            Success = true,
            Message = Messages.OrdersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 2. GET CUSTOMERS
    // ============================================================
    [HttpGet("customers")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> GetCustomers([FromQuery] int? factoryId, [FromQuery] PaginationParams pagination)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
        }

        if (currentRole == UserRole.FactoryEmployee)
        {
            var callerFactoryId = User.GetFactoryId();
            if (callerFactoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
            }
            factoryId = callerFactoryId;
        }

        var result = await _orderService.GetCustomersAsync(factoryId, pagination);

        return Ok(new ApiResponse<PagedResult<CustomerSummaryDto>>
        {
            Success = true,
            Message = Messages.CustomersRetrievedSuccessfully,
            Data = result
        });
    }

    // ============================================================
    // 3. GET BY ID
    // ============================================================
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var order = await _orderService.GetByIdAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<OrderDetailsDto>
        {
            Success = true,
            Message = Messages.OrderRetrievedSuccessfully,
            Data = order
        });
    }

    // ============================================================
    // 4. CREATE
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Client,Admin,FactoryEmployee")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var result = await _orderService.CreateAsync(dto, callerId, callerRole, callerFactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.OrderId },
            new ApiResponse<OrderDetailsDto>
            {
                Success = true,
                Message = Messages.OrderCreatedSuccess,
                Data = result
            });
    }

    // ============================================================
    // 5. UPDATE
    // ============================================================
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        var result = await _orderService.UpdateOrderAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<OrderDto>
        {
            Success = true,
            Message = "تم تحديث الطلب بنجاح.",
            Data = result
        });
    }

    // ============================================================
    // 6. CREATE PHONE ORDER
    // ============================================================
    [HttpPost("phone-order")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> CreatePhoneOrder([FromBody] PhoneOrderDto dto)
    {
        if (!User.TryGetRole(out var currentRole))
        {
            return Unauthorized(new ApiResponse<object> { Success = false, Message = Messages.UserRoleNotFound, Data = null });
        }

        int employeeFactoryId;

        if (currentRole == UserRole.FactoryEmployee)
        {
            var factoryId = User.GetFactoryId();
            if (factoryId == null)
            {
                return Unauthorized(new ApiResponse<object>
                { Success = false, Message = "لم يتم العثور على المصنع المرتبط بالمستخدم.", Data = null });
            }
            employeeFactoryId = factoryId.Value;
        }
        else
        {
            employeeFactoryId = dto.FactoryId;
        }

        var result = await _orderService.CreatePhoneOrderAsync(dto, employeeFactoryId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.OrderId },
            new ApiResponse<OrderDetailsDto>
            {
                Success = true,
                Message = Messages.OrderCreatedSuccess,
                Data = result
            });
    }

    // ============================================================
    // 7. SET PRICE
    // ============================================================
    [HttpPut("{id:int}/price")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> SetPrice(int id, [FromBody] SetPriceDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.SetPriceAsync(id, dto.UnitPrice, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم حفظ السعر بنجاح.", Data = null });
    }

    // ============================================================
    // 8. APPROVE
    // ============================================================
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Approve(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.ApproveOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تمت موافقة العميل بنجاح.", Data = null });
    }

    // ============================================================
    // 9. UPDATE STATUS
    // ============================================================
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.UpdateStatusAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = Messages.OrderStatusUpdated, Data = null });
    }

    // ============================================================
    // 10. ASSIGN DRIVER ✅
    // ============================================================
    [HttpPut("{id:int}/assign-driver")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.AssignDriverAsync(id, dto, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = Messages.DriverAssignedSuccessfully, Data = null });
    }

    // ============================================================
    // 11. START DELIVERY ✅ (تم إضافة Driver)
    // ============================================================
    [HttpPut("{id:int}/start-delivery")]
    [Authorize(Roles = "Admin,FactoryEmployee,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> StartDelivery(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.StartDeliveryAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم بدء التوصيل بنجاح.", Data = null });
    }

    // ============================================================
    // 12. DELIVER ✅ (تم إضافة Driver)
    // ============================================================
    [HttpPut("{id:int}/deliver")]
    [Authorize(Roles = "Admin,FactoryEmployee,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> Deliver(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.DeliverOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم تسليم الطلب بنجاح.", Data = null });
    }

    // ============================================================
    // 13. CLOSE
    // ============================================================
    [HttpPut("{id:int}/close")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Close(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.CloseOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم إغلاق الطلب بنجاح.", Data = null });
    }

    // ============================================================
    // 14. REJECT
    // ============================================================
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,FactoryEmployee")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderDto dto)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.RejectOrderAsync(id, dto.Reason, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم رفض الطلب بنجاح.", Data = null });
    }

    // ============================================================
    // 15. CANCEL ✅ (تم إضافة Driver)
    // ============================================================
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Admin,FactoryEmployee,Client,Driver")]  // ✅ Driver مُضاف
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetCallerContext(out var callerId, out var callerRole, out var callerFactoryId, out var err))
            return err!;

        await _orderService.CancelOrderAsync(id, callerId, callerRole, callerFactoryId);

        return Ok(new ApiResponse<object> { Success = true, Message = "تم إلغاء الطلب بنجاح.", Data = null });
    }
}

public class RejectOrderDto
{
    public string? Reason { get; set; }
}