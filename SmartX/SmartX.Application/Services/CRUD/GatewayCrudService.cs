using SmartX.Application.Commands.Gateway;
using SmartX.Application.Queries.Gateway;
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

    public GatewayCrudService(
        GetGatewaysHandler getGateways,
        GetGatewayByIdHandler getGatewayById,
        CreateGatewayHandler createGateway,
        UpdateGatewayHandler updateGateway,
        DeleteGatewayHandler deleteGateway)
    {
        _getGateways = getGateways;
        _getGatewayById = getGatewayById;
        _createGateway = createGateway;
        _updateGateway = updateGateway;
        _deleteGateway = deleteGateway;
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

    public Task<Result<Guid>> CreateAsync(
        CreateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        return _createGateway.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> UpdateAsync(
        UpdateGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        return _updateGateway.HandleAsync(
            command,
            cancellationToken);
    }

    public Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _deleteGateway.HandleAsync(
            new DeleteGatewayCommand
            {
                Id = id
            },
            cancellationToken);
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