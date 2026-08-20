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

    public UserCrudService(
        GetUsersHandler getUsers,
        GetUserByIdHandler getUserById,
        GetUserByFirebaseUidHandler getUserByFirebaseUid,
        CreateUserHandler createUser,
        UpdateUserHandler updateUser,
        DeleteUserHandler deleteUser)
    {
        _getUsers = getUsers;
        _getUserById = getUserById;
        _getUserByFirebaseUid = getUserByFirebaseUid;
        _createUser = createUser;
        _updateUser = updateUser;
        _deleteUser = deleteUser;
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

    public Task<Result<Guid>> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        return _createUser.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> UpdateAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        return _updateUser.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _deleteUser.HandleAsync(
            new DeleteUserCommand
            {
                UserId = id
            },
            cancellationToken);
    }
}