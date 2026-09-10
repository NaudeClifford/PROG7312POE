using SmartX.Shared.Mapping;
using FirebaseAdmin.Auth;
using SmartX.Application.Authentication;
using SmartX.Application.Requests.User;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;
using System.Security.Claims;

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
        IFirebaseTokenService firebaseTokenService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _auditLog = auditLog;
        _firebaseTokenService = firebaseTokenService;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users =
            await _userRepository.GetAllAsync(
                cancellationToken);

        var dtos =
            _mapper.Map<List<UserDto>>(users);

        return Result<IReadOnlyList<UserDto>>.Ok(dtos);
    }

    public async Task<Result<UserDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Result<UserDto>.Fail(
                "User ID is required.");

        var targetUser =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (targetUser is null)
            return Result<UserDto>.Fail(
                "User not found.");

        if (!CanAccessUser(user, targetUser))
            return Result<UserDto>.Fail(
                "You do not have access to this user.");

        var dto =
            _mapper.Map<UserDto>(targetUser);

        return Result<UserDto>.Ok(dto);
    }

    public async Task<Result<UserDto>> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return Result<UserDto>.Fail(
                "Firebase UID is required.");

        var targetUser =
            await _userRepository.GetByFirebaseUidAsync(
                firebaseUid,
                cancellationToken);

        if (targetUser is null)
            return Result<UserDto>.Fail(
                "User not found.");

        var dto =
            _mapper.Map<UserDto>(targetUser);

        return Result<UserDto>.Ok(dto);
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetByCompanyIdAsync(
        Guid companyId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return Result<IReadOnlyList<UserDto>>.Fail(
                "Company ID is required.");

        if (!CanAccessCompany(user, companyId))
            return Result<IReadOnlyList<UserDto>>.Fail(
                "You do not have access to this company.");

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
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<Guid>.Fail(
                "Request is required.");

        if (request.CompanyId == Guid.Empty)
            return Result<Guid>.Fail(
                "Company ID is required.");

        if (!CanManageCompany(user, request.CompanyId))
            return Result<Guid>.Fail(
                "You do not have access to this company.");

        if (!IsSuperAdmin(user) &&
            request.Role == UserRole.SuperAdmin)
        {
            return Result<Guid>.Fail(
                "Administrators cannot create SuperAdmin users.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<Guid>.Fail(
                "A valid email address is required.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<Guid>.Fail(
                "Display name is required.");

        if (string.IsNullOrWhiteSpace(request.FirebaseUid))
            return Result<Guid>.Fail(
                "Firebase UID is required.");

        var existingUser =
            await _userRepository.GetByFirebaseUidAsync(
                request.FirebaseUid,
                cancellationToken);

        if (existingUser is not null)
            return Result<Guid>.Fail(
                "A user with this Firebase UID already exists.");

        var now = DateTime.UtcNow;

        var newUser = new User
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
            newUser,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: newUser.Id,
            action: "Created",
            companyId: newUser.CompanyId,
            details: "User created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(newUser.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateUserRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<bool>.Fail(
                "Request is required.");

        if (request.Id == Guid.Empty)
            return Result<bool>.Fail(
                "User ID is required.");

        if (request.CompanyId == Guid.Empty)
            return Result<bool>.Fail(
                "Company ID is required.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Result<bool>.Fail(
                "Display name is required.");

        if (string.IsNullOrWhiteSpace(request.FirebaseUid))
            return Result<bool>.Fail(
                "Firebase UID is required.");

        var targetUser =
            await _userRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

        if (targetUser is null)
            return Result<bool>.Fail(
                "User not found.");

        if (!CanManageUser(user, targetUser))
            return Result<bool>.Fail(
                "You do not have permission to modify this user.");

        if (!IsSuperAdmin(user) &&
            request.Role == UserRole.SuperAdmin)
        {
            return Result<bool>.Fail(
                "Administrators cannot assign the SuperAdmin role.");
        }

        if (!CanManageCompany(user, request.CompanyId))
            return Result<bool>.Fail(
                "You do not have access to this company.");

        if (targetUser.CompanyId != request.CompanyId)
            return Result<bool>.Fail(
                "You cannot move this user to another company.");

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

        targetUser.FirebaseUid =
            request.FirebaseUid;

        targetUser.DisplayName =
            request.DisplayName;

        targetUser.Role =
            request.Role;

        targetUser.IsActive =
            request.IsActive;

        targetUser.UpdatedAt =
            DateTime.UtcNow;

        await _userRepository.UpdateAsync(
            targetUser,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "User",
            entityId: targetUser.Id,
            action: "Updated",
            companyId: targetUser.CompanyId,
            details: "User updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Result<bool>.Fail(
                "User ID is required.");

        var targetUser =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (targetUser is null)
            return Result<bool>.Fail(
                "User not found.");

        if (!CanManageUser(user, targetUser))
            return Result<bool>.Fail(
                "You do not have permission to delete this user.");

        if (targetUser.Role == UserRole.SuperAdmin)
            return Result<bool>.Fail(
                "SuperAdmin users cannot be deleted.");

        if (string.IsNullOrWhiteSpace(targetUser.FirebaseUid))
            return Result<bool>.Fail(
                "The user does not have a Firebase UID.");

        try
        {
            await _firebaseTokenService.DeleteUserAsync(
                targetUser.FirebaseUid,
                cancellationToken);

            await _userRepository.DeleteAsync(
                id,
                cancellationToken);

            await _auditLog.LogAsync(
                entityType: "User",
                entityId: targetUser.Id,
                action: "Deleted",
                companyId: targetUser.CompanyId,
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

    private static bool IsSuperAdmin(
        ClaimsPrincipal user)
    {
        return user.IsInRole(UserRole.SuperAdmin.ToString());
    }

    private static bool IsAdministrator(
        ClaimsPrincipal user)
    {
        return user.IsInRole(UserRole.Administrator.ToString());
    }

    private static Guid? GetUserCompanyId(
        ClaimsPrincipal user)
    {
        var claim =
            user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(claim, out var companyId))
            return null;

        return companyId == Guid.Empty
            ? null
            : companyId;
    }

    private static bool CanAccessCompany(
        ClaimsPrincipal user,
        Guid companyId)
    {
        if (IsSuperAdmin(user))
            return true;

        if (!IsAdministrator(user))
            return false;

        var userCompanyId =
            GetUserCompanyId(user);

        return userCompanyId == companyId;
    }

    private static bool CanManageCompany(
        ClaimsPrincipal user,
        Guid companyId)
    {
        return CanAccessCompany(user, companyId);
    }

    private static bool CanAccessUser(
        ClaimsPrincipal user,
        User targetUser)
    {
        if (IsSuperAdmin(user))
            return true;

        if (!IsAdministrator(user))
            return false;

        if (targetUser.Role == UserRole.SuperAdmin)
            return false;

        return GetUserCompanyId(user) == targetUser.CompanyId;
    }

    private static bool CanManageUser(
        ClaimsPrincipal user,
        User targetUser)
    {
        return CanAccessUser(user, targetUser);
    }
}
