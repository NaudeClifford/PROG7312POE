
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Company;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, SuperAdmin")]
public class CompaniesController : ControllerBase
{
    private readonly CompanyCrudService _crud;

    public CompaniesController(
        CompanyCrudService crud)
    {
        _crud = crud;
    }

    [HttpGet] 
    [Authorize(Roles = "SuperAdmin")]

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
    [Authorize(Roles = "SuperAdmin")]

    public async Task<IActionResult> Create(
        CreateCompanyRequest request,
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
    [Authorize(Roles = "SuperAdmin")]

    public async Task<IActionResult> Update(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result = await _crud.UpdateAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Company not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")] 
    [Authorize(Roles = "SuperAdmin")]

    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.DeleteAsync(
            id,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Company not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}