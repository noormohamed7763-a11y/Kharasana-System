using Asp.Versioning;
using Kharasana.API.Common;
using Kharasana.API.Extensions;
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
        int? factoryId = caller.Role == Kharasana.Domain.Enums.UserRole.Admin ? null : caller.FactoryId;
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
        int? factoryId = caller.Role == Kharasana.Domain.Enums.UserRole.Admin ? null : caller.FactoryId;
        var material = await _rawMaterialService.GetByIdAsync(id, factoryId);

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
        var factoryId = caller.Role == Kharasana.Domain.Enums.UserRole.Admin
            ? dto.FactoryId
            : caller.FactoryId;
        if (!factoryId.HasValue)
            return BadRequest(ApiResponse.Fail(Messages.FactoryNotFound));

        var created = await _rawMaterialService.CreateAsync(dto, factoryId.Value);

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
        int? factoryId = caller.Role == Kharasana.Domain.Enums.UserRole.Admin ? null : caller.FactoryId;
        await _rawMaterialService.UpdateStockAsync(id, dto.QuantityChange, factoryId);

        return Ok(ApiResponse.Ok(Messages.RawMaterialStockUpdatedSuccessfully));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var caller = User.GetCallerContext();
        int? factoryId = caller.Role == Kharasana.Domain.Enums.UserRole.Admin ? null : caller.FactoryId;
        await _rawMaterialService.DeleteAsync(id, factoryId);

        return Ok(ApiResponse.Ok(Messages.RawMaterialDeletedSuccessfully));
    }
}
