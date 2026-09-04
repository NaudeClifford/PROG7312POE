using AutoMapper;
using SmartX.Application.Authentication;
using SmartX.Application.Requests.User;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;

namespace SmartX.Application.Services.CRUD;

public class UserCrudService :
    ICrudService<
        UserDto,
        CreateUserRequest,
        UpdateUserRequest>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly AuditLogService _auditLog;
    private readonly IFirebaseTokenService _firebaseTokenService;


    public UserCrudService(
        IUserRepository userRepository,
        IMapper mapper,
        AuditLogService auditLog,
        IFirebaseTokenService firebaseTokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _auditLog = auditLog;
        _firebaseTokenService = firebaseTokenService;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<UserDto>>(users);

        return Result<IReadOnlyList<UserDto>>.Ok(dtos);
    }

    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<UserDto>.Fail(
                "User ID is required.");
        }

        var user = await _userRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Fail(
                "User not found.");
        }

        var dto = _mapper.Map<UserDto>(user);

        return Result<UserDto>.Ok(dto);
    }

    public async Task<Result<UserDto>> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Result<UserDto>.Fail(
                "Firebase UID is required.");
        }

        var user =
            await _userRepository.GetByFirebaseUidAsync(
                firebaseUid,
                cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Fail(
                "User not found.");
        }

        var dto = _mapper.Map<UserDto>(user);

        return Result<UserDto>.Ok(dto);
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return Result<IReadOnlyList<UserDto>>.Fail(
                "Company ID is required.");
        }

        var users =
            await _userRepository.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

        var dtos =
            _mapper.Map<List<UserDto>>(users);

        return Result<IReadOnlyList<UserDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CompanyId == Guid.Empty)
        {
            return Result<Guid>.Fail(
                "Company ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<Guid>.Fail(
                "A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<Guid>.Fail(
                "Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirebaseUid))
        {
            return Result<Guid>.Fail(
                "Firebase UID is required.");
        }

        var existingUser =
            await _userRepository.GetByFirebaseUidAsync(
                request.FirebaseUid,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<Guid>.Fail(
                "A user with this Firebase UID already exists.");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            FirebaseUid = request.FirebaseUid,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: user.Id,
            action: "Created",
            companyId: user.CompanyId,
            details: "User created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "User ID is required.");
        }

        if (request.CompanyId == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Company ID is required.");
        }


        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<bool>.Fail(
                "Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirebaseUid))
        {
            return Result<bool>.Fail(
                "Firebase UID is required.");
        }

        var user = await _userRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (user is null)
        {
            return Result<bool>.Fail(
                "User not found.");
        }

        var existingUser =
            await _userRepository.GetByFirebaseUidAsync(
                request.FirebaseUid,
                cancellationToken);

        if (existingUser is not null &&
            existingUser.Id != request.Id)
        {
            return Result<bool>.Fail(
                "A user with this Firebase UID already exists.");
        }

        user.CompanyId = request.CompanyId;
        user.FirebaseUid = request.FirebaseUid;
        user.DisplayName = request.DisplayName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        // UpdatedAt is used by WPF synchronization.
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(
            user,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: user.Id,
            action: "Updated",
            companyId: user.CompanyId,
            details: "User updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "User ID is required.");
        }

        var user = await _userRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            return Result<bool>.Fail(
                "User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.FirebaseUid))
        {
            return Result<bool>.Fail(
                "The user does not have a Firebase UID.");
        }

        try
        {
            // Delete Firebase Authentication account first.
            await _firebaseTokenService.DeleteUserAsync(
                user.FirebaseUid,
                cancellationToken);

            // Delete SmartX user only after Firebase succeeds.
            await _userRepository.DeleteAsync(
                id,
                cancellationToken);

            await _auditLog.LogAsync(
                entityType: "User",
                entityId: user.Id,
                action: "Deleted",
                companyId: user.CompanyId,
                details: "User deleted.",
                cancellationToken: cancellationToken);

            return Result<bool>.Ok(true);
        }
        catch (FirebaseAuthException ex)
        {
            return Result<bool>.Fail(
                $"Unable to delete the Firebase account: {ex.Message}");
        }
    }
}
