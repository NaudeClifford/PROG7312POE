using SmartX.Application.Commands.Users;
using SmartX.Application.Queries.Users;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class UserCrudService :
    ICrudService<
        UserDto,
        CreateUserCommand,
        UpdateUserCommand>
{
    private readonly GetUsersHandler _getUsers;
    private readonly GetUserByIdHandler _getUserById;
    private readonly CreateUserHandler _createUser;
    private readonly UpdateUserHandler _updateUser;
    private readonly DeleteUserHandler _deleteUser;
    private readonly GetUserByFirebaseUidHandler _getUserByFirebaseUid;
    private readonly AuditLogService _auditLog;

    public UserCrudService(
        GetUsersHandler getUsers,
        GetUserByIdHandler getUserById,
        GetUserByFirebaseUidHandler getUserByFirebaseUid,
        CreateUserHandler createUser,
        UpdateUserHandler updateUser,
        DeleteUserHandler deleteUser,
        AuditLogService auditLog)
    {
        _getUsers = getUsers;
        _getUserById = getUserById;
        _getUserByFirebaseUid = getUserByFirebaseUid;
        _createUser = createUser;
        _updateUser = updateUser;
        _deleteUser = deleteUser;
        _auditLog = auditLog;
    }

    public Task<Result<UserDto>> GetByFirebaseUidAsync(
    string firebaseUid,
    CancellationToken cancellationToken = default)
    {
        return _getUserByFirebaseUid.HandleAsync(
            new GetUserByFirebaseUidQuery
            {
                FirebaseUid = firebaseUid
            },
            cancellationToken);
    }

    public Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _getUsers.HandleAsync(
            new GetUsersQuery(),
            cancellationToken);
    }

    public Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _getUserById.HandleAsync(
            new GetUserByIdQuery
            {
                UserId = id
            },
            cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _createUser.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: result.Data,
            action: "Created",
            companyId: command.CompanyId,
            details: "User created.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _updateUser.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: command.Id,
            action: "Updated",
            companyId: command.CompanyId,
            details: "User updated.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var userResult = await _getUserById.HandleAsync(
            new GetUserByIdQuery
            {
                UserId = id
            },
            cancellationToken);

        if (!userResult.Success)
            return Result<bool>.Fail(
                userResult.Error ?? "Unable to retrieve user.");

        var result = await _deleteUser.HandleAsync(
            new DeleteUserCommand
            {
                UserId = id
            },
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: id,
            action: "Deleted",
            companyId: userResult.Data!.CompanyId,
            details: "User deleted.",
            cancellationToken: cancellationToken);

        return result;
    }
}