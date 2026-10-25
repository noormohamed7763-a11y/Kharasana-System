using Asp.Versioning;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Kharasana.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Kharasana.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,FactoryEmployee")]
public class ProductionBatchesController : ControllerBase
{
    private readonly IProductionBatchService _productionBatchService;

    public ProductionBatchesController(IProductionBatchService productionBatchService)
    {
        _productionBatchService = productionBatchService;
    }

    private int GetFactoryId() => int.Parse(User.FindFirstValue("FactoryId") ?? "0");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductionBatch>>> GetAll()
    {
        var batches = await _productionBatchService.GetFactoryProductionBatchesAsync(GetFactoryId());
        return Ok(batches);
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<ProductionBatch>>> GetByOrder(int orderId)
    {
        var batches = await _productionBatchService.GetByOrderIdAsync(orderId);
        return Ok(batches);
    }

    [HttpPost("order/{orderId}")]
    public async Task<ActionResult<ProductionBatch>> Create(int orderId, [FromBody] ProductionBatch batch)
    {
        var created = await _productionBatchService.CreateAsync(batch, orderId);
        return Ok(created);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ProductionStatus status)
    {
        await _productionBatchService.UpdateStatusAsync(id, status, GetFactoryId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productionBatchService.DeleteAsync(id, GetFactoryId());
        return NoContent();
    }
}
