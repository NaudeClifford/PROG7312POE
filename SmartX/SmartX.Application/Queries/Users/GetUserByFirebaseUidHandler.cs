using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Queries.Users;

public class GetUserByFirebaseUidHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserByFirebaseUidHandler(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> HandleAsync(
        GetUserByFirebaseUidQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.FirebaseUid))
        {
            return Result<UserDto>.Fail(
                "Firebase UID is required.");
        }

        var user =
            await _userRepository.GetByFirebaseUidAsync(
                query.FirebaseUid,
                cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Fail(
                "User not found.");
        }

        var dto = new UserDto
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            FirebaseUid = user.FirebaseUid,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Result<UserDto>.Ok(dto);
    }
}