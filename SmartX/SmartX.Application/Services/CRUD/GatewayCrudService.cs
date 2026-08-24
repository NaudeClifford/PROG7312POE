using SmartX.Application.Commands.Gateway;
using SmartX.Application.Queries.Gateway;
using SmartX.Domain.Entities;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class GatewayCrudService :
    ICrudService<
        GatewayDto,
        CreateGatewayCommand,
        UpdateGatewayCommand>
{
    private readonly GetGatewaysHandler _getGateways;
    private readonly GetGatewayByIdHandler _getGatewayById;
    private readonly CreateGatewayHandler _createGateway;
    private readonly UpdateGatewayHandler _updateGateway;
    private readonly DeleteGatewayHandler _deleteGateway;

    private readonly AuditLogService _auditLog;

    public GatewayCrudService(
        GetGatewaysHandler getGateways,
        GetGatewayByIdHandler getGatewayById,
        CreateGatewayHandler createGateway,
        UpdateGatewayHandler updateGateway,
        DeleteGatewayHandler deleteGateway,
                AuditLogService auditLog)
    {
        _getGateways = getGateways;
        _getGatewayById = getGatewayById;
        _createGateway = createGateway;
        _updateGateway = updateGateway;
        _deleteGateway = deleteGateway;
        _auditLog = auditLog;

    }

    public Task<Result<IReadOnlyList<GatewayDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _getGateways.HandleAsync(
            new GetGatewaysQuery(),
            cancellationToken);
    }

    public Task<Result<GatewayDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _getGatewayById.HandleAsync(
            new GetGatewayByIdQuery
            {
                Id = id
            },
            cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {

        var result =
            await _createGateway.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: result.Data,
            action: "Created",
            companyId: command.CompanyId,
            details: "Gateway created.",
            cancellationToken: cancellationToken);

        return result;

    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _updateGateway.HandleAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: command.Id,
            action: "Updated",
            companyId: command.CompanyId,
            details: "Gateway updated.",
            cancellationToken: cancellationToken);

        return result;
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var gatewayResult = await _getGatewayById.HandleAsync(new
            GetGatewayByIdQuery { Id = id }, 
            cancellationToken); 
        
        if (!gatewayResult.Success) return Result<bool>.Fail(gatewayResult.Error ??
                "Unable to retrieve gateway."); 

        var result =
            await _deleteGateway.HandleAsync(
            new DeleteGatewayCommand
            {
                Id = id
            },
            cancellationToken);

        if (!result.Success)
            return result;

        await _auditLog.LogAsync(
            entityType: "Gateway",
            entityId: id,
            action: "Deleted",
            companyId: gatewayResult.Data!.CompanyId,
            details: "Gateway deleted.",
            cancellationToken: cancellationToken);

        return result;
    }

    public Task<Result<IReadOnlyList<GatewayDto>>> GetByCompanyIdAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        return _getGateways.HandleAsync(
            new GetGatewaysQuery
            {
                CompanyId = companyId
            },
            cancellationToken);
    }
}