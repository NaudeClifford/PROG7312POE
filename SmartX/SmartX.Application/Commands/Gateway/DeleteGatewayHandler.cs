using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Gateway;

public class DeleteGatewayHandler
{
    private readonly IGatewayRepository _gatewayRepository;

    public DeleteGatewayHandler(IGatewayRepository gatewayRepository)
    {
        _gatewayRepository = gatewayRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        DeleteGatewayCommand command,
        CancellationToken cancellationToken = default)
    {

        if (command.Id == Guid.Empty)
        {
            return Result<bool>.Fail("gateway ID is required.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail("gateway not found.");
        }

        await _gatewayRepository.DeleteAsync(
            command.Id,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}