using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Gateway;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, Technician")]
public class GatewaysController : ControllerBase
{
    private readonly GatewayCrudService _crud;

    public GatewaysController(
        GatewayCrudService crud)
    {
        _crud = crud;
    }

    [HttpGet]  
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetAllAsync(
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetByIdAsync(
            id,
            cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]  
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Create(
        CreateGatewayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _crud.CreateAsync(
            request,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Update(
        Guid id,
        UpdateGatewayRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result = await _crud.UpdateAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Gateway not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.DeleteAsync(
            id,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Gateway not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("company/{companyId:guid}")]
    public async Task<IActionResult> GetByCompanyId(
    Guid companyId,
    CancellationToken cancellationToken)
    {
        var result = await _crud.GetByCompanyIdAsync(
            companyId,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}