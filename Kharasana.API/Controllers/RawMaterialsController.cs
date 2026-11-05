using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.Application.Common;
using Kharasana.Application.DTOs.RawMaterial;
using Kharasana.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kharasana.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,FactoryEmployee")]
public class RawMaterialsController : ControllerBase
{
    private readonly IRawMaterialService _rawMaterialService;

    public RawMaterialsController(IRawMaterialService rawMaterialService)
    {
        _rawMaterialService = rawMaterialService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caller = User.GetCallerContext();
        int factoryId = caller.FactoryId ?? 0;
        var materials = await _rawMaterialService.GetFactoryMaterialsAsync(factoryId);

        return Ok(new ApiResponse<IEnumerable<RawMaterialDto>>
        {
            Success = true,
            Message = Messages.RawMaterialsRetrievedSuccessfully,
            Data = materials
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var caller = User.GetCallerContext();
        var material = await _rawMaterialService.GetByIdAsync(id, caller.FactoryId ?? 0);

        return Ok(new ApiResponse<RawMaterialDto>
        {
            Success = true,
            Message = Messages.RawMaterialRetrievedSuccessfully,
            Data = material
        });
    }

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilter<CreateRawMaterialDto>))]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialDto dto)
    {
        var caller = User.GetCallerContext();
        var created = await _rawMaterialService.CreateAsync(dto, caller.FactoryId ?? 0);

        return Ok(new ApiResponse<RawMaterialDto>
        {
            Success = true,
            Message = Messages.RawMaterialCreatedSuccessfully,
            Data = created
        });
    }

    [HttpPut("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateRawMaterialStockDto dto)
    {
        var caller = User.GetCallerContext();
        await _rawMaterialService.UpdateStockAsync(id, dto.QuantityChange, caller.FactoryId ?? 0);

        return Ok(ApiResponse.Ok(Messages.RawMaterialStockUpdatedSuccessfully));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();
        await _rawMaterialService.DeleteAsync(id, caller.FactoryId ?? 0);

        return Ok(ApiResponse.Ok(Messages.RawMaterialDeletedSuccessfully));
    }
}
