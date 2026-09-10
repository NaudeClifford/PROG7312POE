using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Company;
using SmartX.Application.Services.CRUD;
using SmartX.Application.Services.Registration;
using SmartX.Domain.Enums;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.GetByIdAsync(
            id,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            _ =>
                BadRequest(result)
        };
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Request is required.");

        var result = await _crud.CreateAsync(
            request,
            User,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Company ID is required.");

        if (request is null)
            return BadRequest("Request is required.");

        request.Id = id;

        var result = await _crud.UpdateAsync(
            request,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            _ =>
                BadRequest(result)
        };
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.DeleteAsync(
            id,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            _ =>
                BadRequest(result)
        };
    }


    [HttpPost("{companyId:guid}/deletion-request")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> RequestDeletion(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.RequestDeletionAsync(
            companyId,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            "A deletion request already exists." =>
                Conflict(result),

            "Company ID is required." =>
                BadRequest(result),

            _ =>
                BadRequest(result)
        };
    }

    [HttpPost("{companyId:guid}/deletion-request/cancel")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> CancelDeletion(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.CancelDeletionAsync(
            companyId,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            "No deletion request exists." =>
                BadRequest(result),

            _ =>
                BadRequest(result)
        };
    }

    [HttpGet("{companyId:guid}/configuration")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> GetConfiguration(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.GetConfigurationAsync(
            companyId,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            _ =>
                BadRequest(result)
        };
    }

    [HttpPut("{companyId:guid}/configuration")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> UpdateConfiguration(
        Guid companyId,
        UpdateCompanyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        if (request is null)
            return BadRequest("Request is required.");

        request.CompanyId = companyId;

        var result = await _crud.UpdateConfigurationAsync(
            request,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            _ =>
                BadRequest(result)
        };
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterCompanyRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Request is required.");

        var result = await _service.RegisterAsync(
            request,
            cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpPost("{companyId:guid}/onboarding/complete")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public async Task<IActionResult> CompleteOnboarding(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _service.CompleteOnboardingAsync(
            companyId,
            User,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company was not found." =>
                NotFound(result),

            "You do not have access to this company." =>
                Forbid(),

            "Company ID is required." =>
                BadRequest(result),

            "Company is inactive." =>
                BadRequest(result),

            _ =>
                BadRequest(result)
        };
    }
    
    [HttpDelete("{id:guid}/guest")]
    [Authorize]
    public async Task<IActionResult> DeleteGuest(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Company ID is required.");

        var result = await _crud.DeleteGuestAsync(
            id,
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Company not found." =>
                NotFound(result),

            _ =>
                BadRequest(result)
        };
    }




}