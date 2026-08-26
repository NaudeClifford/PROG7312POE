using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Users;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserCrudService _crud;

    public UsersController(
        UserCrudService crud)
    {
        _crud = crud;
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetAllAsync(
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // =========================================================
    // GET BY SMARTX USER ID
    // =========================================================

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetByIdAsync(
                id,
                cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    // =========================================================
    // GET BY FIREBASE UID
    // =========================================================

    [HttpGet("firebase/{firebaseUid}")]
    public async Task<IActionResult> GetByFirebaseUid(
        string firebaseUid,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return BadRequest(new
            {
                Success = false,
                Error = "Firebase UID is required."
            });
        }

        var result =
            await _crud.GetByFirebaseUidAsync(
                firebaseUid,
                cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    // =========================================================
    // CREATE
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.CreateAsync(
                command,
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        var result =
            await _crud.UpdateAsync(
                command,
                cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "User not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    // =========================================================
    // DELETE
    // =========================================================

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.DeleteAsync(
                id,
                cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "User not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}