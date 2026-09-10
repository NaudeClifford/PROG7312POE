using SmartX.Shared.Mapping;
using FluentValidation;
using SmartX.Application.Requests.Gateway;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;
using System.ComponentModel.Design;
using System.Security.Claims;

namespace SmartX.Application.Services.CRUD;

public class GatewayCrudService :
    ICrudService<
        GatewayDto,
        CreateGatewayRequest,
        UpdateGatewayRequest>
{
    private readonly IGatewayRepository _gatewayRepository;

    private readonly IValidator<CreateGatewayRequest>
        _createValidator;

    private readonly IValidator<UpdateGatewayRequest>
        _updateValidator;

    private readonly IMapper _mapper;

    private readonly AuditLogService _auditLog;

    public GatewayCrudService(
        IGatewayRepository gatewayRepository,
        IValidator<CreateGatewayRequest> createValidator,
        IValidator<UpdateGatewayRequest> updateValidator,
        IMapper mapper,
        AuditLogService auditLog)
    {
        _gatewayRepository = gatewayRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _auditLog = auditLog;
    }

    public async Task<Result<IReadOnlyList<GatewayDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var gateways = await _gatewayRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<GatewayDto>>(gateways);

        return Result<IReadOnlyList<GatewayDto>>.Ok(dtos);
    }

    public async Task<Result<GatewayDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Result<GatewayDto>.Fail(
                "Gateway ID is required.");

        var gateway = await _gatewayRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (gateway is null)
            return Result<GatewayDto>.Fail(
                "Gateway not found.");

        if (!CanAccessCompany(user, gateway.CompanyId))
            return Result<GatewayDto>.Fail(
                "You do not have access to this gateway.");

        var dto = _mapper.Map<GatewayDto>(gateway);

        return Result<GatewayDto>.Ok(dto);
    }


    public async Task<Result<Guid>> CreateAsync(
        CreateGatewayRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<Guid>.Fail(
                "Request is required.");

        if (request.CompanyId == Guid.Empty)
            return Result<Guid>.Fail(
                "Company ID is required.");

        if (!CanAccessCompany(user, request.CompanyId))
            return Result<Guid>.Fail(
                "You do not have access to this company.");

        var validationResult = await _createValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors.Select(
                    x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var now = DateTime.UtcNow;

        var gateway = new Gateway
        {
            Id = Guid.NewGuid(),

            CompanyId = request.CompanyId,

            Name = request.Name,
            Description = request.Description,

            SerialNumber = request.SerialNumber,
            IpAddress = request.IpAddress,

            IsActive = true,

            CreatedAt = now,
            UpdatedAt = now
        };

        await _gatewayRepository.AddAsync(
            gateway,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: gateway.Id,
            action: "Created",
            companyId: gateway.CompanyId,
            details: "Gateway created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(gateway.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateGatewayRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<bool>.Fail(
                "Request is required.");

        if (request.Id == Guid.Empty)
            return Result<bool>.Fail(
                "Gateway ID is required.");

        var validationResult =
            await _updateValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors.Select(
                    x => x.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (gateway is null)
            return Result<bool>.Fail(
                "Gateway not found.");

        if (!CanAccessCompany(user, gateway.CompanyId))
            return Result<bool>.Fail(
                "You do not have access to this gateway.");

        gateway.Name = request.Name;
        gateway.Description = request.Description;
        gateway.SerialNumber = request.SerialNumber;
        gateway.IpAddress = request.IpAddress;
        gateway.IsActive = request.IsActive;

        gateway.UpdatedAt = DateTime.UtcNow;

        await _gatewayRepository.UpdateAsync(gateway, cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: gateway.Id,
            action: "Updated",
            companyId: gateway.CompanyId,
            details: "Gateway updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return Result<bool>.Fail(
                "Gateway ID is required.");

        var gateway = await _gatewayRepository.GetByIdAsync(id, cancellationToken);

        if (gateway is null) return Result<bool>.Fail(
                "Gateway not found.");

        if (!CanAccessCompany(user, gateway.CompanyId))
            return Result<bool>.Fail(
                "You do not have access to this gateway.");

        await _gatewayRepository.DeleteAsync(
            id,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: id,
            action: "Deleted",
            companyId: gateway.CompanyId,
            details: "Gateway deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<IReadOnlyList<GatewayDto>>> GetByCompanyIdAsync(
        Guid companyId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return Result<IReadOnlyList<GatewayDto>>.Fail(
                "Company ID is required.");

        if (!CanAccessCompany(user, companyId))
            return Result<IReadOnlyList<GatewayDto>>.Fail(
                "You do not have access to this company.");

        var gateways =
            await _gatewayRepository.GetByCompanyIdAsync(
                companyId,
                cancellationToken);

        var dtos = _mapper.Map<List<GatewayDto>>(gateways);

        return Result<IReadOnlyList<GatewayDto>>.Ok(dtos);
    }

    private static bool CanAccessCompany(
            ClaimsPrincipal user,
            Guid companyId)
    {
        if (companyId == Guid.Empty)
            return false;

        var isAdministrator =
            user.IsInRole("Administrator");

        var isTechnician =
            user.IsInRole("Technician");

        if (!isAdministrator && !isTechnician)
            return false;

        var claim =
            user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(claim, out var userCompanyId))
            return false;

        return userCompanyId == companyId;
    }

}
