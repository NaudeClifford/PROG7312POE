
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Company;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Services.Registration;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, SuperAdmin")]
public class CompaniesController : ControllerBase
{
    private readonly CompanyCrudService _crud;
    private readonly RegistrationService _service;

    public CompaniesController(
        CompanyCrudService crud,
        RegistrationService service)
    {
        _crud = crud;
        _service = service;
    }

    [HttpGet] 
    [Authorize(Roles = "SuperAdmin")]

    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetAllAsync(
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetByIdAsync(id, cancellationToken);

        return result.Success
            ? Ok(result)
            : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _crud.CreateAsync( request, cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result = await _crud.UpdateAsync(
            request,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Company not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpPost("{companyId:guid}/deletion-request")]
    public async Task<IActionResult> RequestDeletion(
    Guid companyId,
    CancellationToken cancellationToken)
    {
        var result = await _crud.RequestDeletionAsync(
            companyId,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." => NotFound(result),
            "Company ID is required." => BadRequest(result),
            "A deletion request already exists." => Conflict(result),
            _ => BadRequest(result)
        };
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

        if (result.Success)
            return Ok(result);

        return result.Error == "Company not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    // Configuration

    [HttpGet("{companyId:guid}/configuration")]
    public async Task<IActionResult> GetConfiguration(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetConfigurationAsync(
            companyId,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPut("{companyId:guid}/configuration")]
    public async Task<IActionResult> UpdateConfiguration(
        Guid companyId,
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        request.CompanyId = companyId;

        var result = await _crud.UpdateConfigurationAsync(
            request,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // Registration

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(
            request,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // Onboarding

    [HttpPost("{companyId:guid}/onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _service.CompleteOnboardingAsync(
            companyId,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }




}