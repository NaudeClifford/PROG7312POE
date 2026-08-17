using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Users;
using SmartX.Application.Queries.Users;
using SmartX.Shared.Models;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GetUsersHandler _getUsersHandler;
    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly CreateUserHandler _createUserHandler;
    private readonly UpdateUserHandler _updateUserHandler;
    private readonly DeleteUserHandler _deleteUserHandler;

    public UsersController(
        GetUsersHandler getUserHandler,
        GetUserByIdHandler getUserByIdHandler,
        CreateUserHandler createUserHandler,
        UpdateUserHandler updateUserHandler,
        DeleteUserHandler deleteUserHandler)
    {
        _getUsersHandler = getUserHandler;
        _getUserByIdHandler = getUserByIdHandler;
        _createUserHandler = createUserHandler;
        _updateUserHandler = updateUserHandler;
        _deleteUserHandler = deleteUserHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _getUsersHandler.HandleAsync(
            new GetUsersQuery(),
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
        var result = await _getUserByIdHandler.HandleAsync(
            new GetUserByIdQuery
            {
                UserId = id
            },
            cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _createUserHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(
                Result<bool>.Fail(
                    "The route ID does not match the command ID."));
        }

        var result = await _updateUserHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result.Error == "User not found."
                ? NotFound(result)
                : BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand
        {
            UserId = id
        };

        var result = await _deleteUserHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}