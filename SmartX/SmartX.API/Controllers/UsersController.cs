using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.User;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserCrudService _crud;

    public UsersController(
        UserCrudService crud)
    {
        _crud = crud;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetAllAsync(
                cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetByIdAsync(
                id,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "User not found."
            ? NotFound(result)
            : Forbid();
    }

    [HttpGet("firebase/{firebaseUid}")]
    [Authorize(Roles = "SuperAdmin, Administrator, Technician")]
    public async Task<IActionResult> GetByFirebaseUid(
        string firebaseUid,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetByFirebaseUidAsync(
                firebaseUid,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : NotFound(result);
    }

    [HttpGet("company/{companyId:guid}")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    public async Task<IActionResult> GetByCompanyId(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetByCompanyIdAsync(
                companyId,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return Forbid();
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.CreateAsync(
                request,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result =
            await _crud.UpdateAsync(
                request,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        if (result.Error == "User not found.")
            return NotFound(result);



#pragma warning disable CS8602 // Dereference of a possibly null reference.
        if (result.Error.Contains("permission") ||
            result.Error.Contains("access"))
            return Forbid();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Administrator")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.DeleteAsync(
                id,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        if (result.Error == "User not found.")
            return NotFound(result);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        if (result.Error.Contains("permission") ||
            result.Error.Contains("SuperAdmin"))
            return Forbid();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return BadRequest(result);
    }
}
