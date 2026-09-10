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
        var companyId = GetUserCompanyId();

        if (companyId is null) return Forbid();

        var result = await _crud.GetByCompanyIdAsync(companyId.Value, User, cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Gateway ID is required.");

        var companyId = GetUserCompanyId();

        if (companyId is null)
            return Forbid();

        var result = await _crud.GetByIdAsync(
            id,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Gateway not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpPost]  
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Create(
        CreateGatewayRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = GetUserCompanyId();

        if (companyId is null)
            return Forbid();

        // Never trust the CompanyId supplied by the client.
        request.CompanyId = companyId.Value;

        var result = await _crud.CreateAsync(
            request, User,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Update(
        Guid id,
        UpdateGatewayRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Gateway ID is required.");

        var companyId = GetUserCompanyId();

        if (companyId is null)
            return Forbid();

        request.Id = id;

        // The authenticated user's company is authoritative.
        request.CompanyId = companyId.Value;

        var result = await _crud.UpdateAsync(
            request,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Gateway not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]

    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Gateway ID is required.");

        var companyId = GetUserCompanyId();

        if (companyId is null)
            return Forbid();

        var result = await _crud.DeleteAsync(
            id,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Gateway not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpGet("company/{companyId:guid}")]
    public async Task<IActionResult> GetByCompanyId(
    Guid companyId,
    CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        var userCompanyId = GetUserCompanyId();

        if (userCompanyId is null)
            return Forbid();

        // Normal users may only access their own company.
        if (userCompanyId.Value != companyId)
            return Forbid();

        var result = await _crud.GetByCompanyIdAsync(
            companyId, User,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    private Guid? GetUserCompanyId()
    {
        var claim = User.FindFirst("CompanyId");

        if (claim is null ||
            !Guid.TryParse(claim.Value, out var companyId) ||
            companyId == Guid.Empty)
                return null;
        
        return companyId;
    }
}