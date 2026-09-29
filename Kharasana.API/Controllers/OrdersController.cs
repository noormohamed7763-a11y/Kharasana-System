using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

// الأجزاء الأخرى من هذا المتحكّم:
//   OrdersController.Read.cs     — GetAll / GetByDriver / GetCustomers / GetCustomer / GetById.
//   OrdersController.Commands.cs — Create / Update / CreatePhoneOrder / Delete.
//   OrdersController.Workflow.cs — التسعير والموافقة والحالة والإسناد والتوصيل والإغلاق والرفض والإلغاء.
/// <summary>
/// إدارة طلبات الخرسانة بدورة حياتها الكاملة: إنشاء، تسعير، موافقة عميل،
/// إسناد سائق، توصيل، إغلاق، رفض وإلغاء — مع عزل البيانات بحسب دور المتصل.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public partial class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }
}
