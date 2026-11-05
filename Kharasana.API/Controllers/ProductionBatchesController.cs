using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.ProductionBatch;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,FactoryEmployee")]
public class ProductionBatchesController : ControllerBase
{
    private readonly IProductionBatchService _productionBatchService;

    public ProductionBatchesController(IProductionBatchService productionBatchService)
    {
        _productionBatchService = productionBatchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caller = User.GetCallerContext();
        var batches = await _productionBatchService.GetFactoryProductionBatchesAsync(caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<IEnumerable<ProductionBatchDto>>
        {
            Success = true,
            Message = Messages.ProductionBatchesRetrievedSuccessfully,
            Data = batches
        });
    }

    [HttpGet("order/{orderId:int}")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var caller = User.GetCallerContext();
        var batches = await _productionBatchService.GetByOrderIdAsync(orderId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<IEnumerable<ProductionBatchDto>>
        {
            Success = true,
            Message = Messages.ProductionBatchesByOrderRetrievedSuccessfully,
            Data = batches
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var caller = User.GetCallerContext();
        var batch = await _productionBatchService.GetByIdAsync(id, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<ProductionBatchDto>
        {
            Success = true,
            Message = Messages.ProductionBatchesRetrievedSuccessfully,
            Data = batch
        });
    }

    [HttpPost("order/{orderId:int}")]
    [ServiceFilter(typeof(ValidationFilter<CreateProductionBatchDto>))]
    public async Task<IActionResult> Create(int orderId, [FromBody] CreateProductionBatchDto dto)
    {
        var caller = User.GetCallerContext();
        var created = await _productionBatchService.CreateAsync(dto, orderId, caller.Role, caller.FactoryId);

        return Ok(new ApiResponse<ProductionBatchDto>
        {
            Success = true,
            Message = Messages.ProductionBatchCreatedSuccessfully,
            Data = created
        });
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ProductionStatus status)
    {
        var caller = User.GetCallerContext();
        await _productionBatchService.UpdateStatusAsync(id, status, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.ProductionBatchStatusUpdatedSuccessfully));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();
        await _productionBatchService.DeleteAsync(id, caller.Role, caller.FactoryId);

        return Ok(ApiResponse.Ok(Messages.ProductionBatchDeletedSuccessfully));
    }
}
