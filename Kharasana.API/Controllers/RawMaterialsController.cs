using Asp.Versioning;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Kharasana.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,FactoryEmployee")]
public class RawMaterialsController : ControllerBase
{
    private readonly IRawMaterialService _rawMaterialService;

    public RawMaterialsController(IRawMaterialService rawMaterialService)
    {
        _rawMaterialService = rawMaterialService;
    }

    private int GetFactoryId() => int.Parse(User.FindFirstValue("FactoryId") ?? "0");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RawMaterial>>> GetAll()
    {
        var materials = await _rawMaterialService.GetFactoryMaterialsAsync(GetFactoryId());
        return Ok(materials);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RawMaterial>> GetById(int id)
    {
        var material = await _rawMaterialService.GetByIdAsync(id, GetFactoryId());
        return Ok(material);
    }

    [HttpPost]
    public async Task<ActionResult<RawMaterial>> Create(RawMaterial material)
    {
        var created = await _rawMaterialService.CreateAsync(material, GetFactoryId());
        return CreatedAtAction(nameof(GetById), new { id = created.RawMaterialId }, created);
    }

    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] decimal quantityChange)
    {
        await _rawMaterialService.UpdateStockAsync(id, quantityChange, GetFactoryId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _rawMaterialService.DeleteAsync(id, GetFactoryId());
        return NoContent();
    }
}
