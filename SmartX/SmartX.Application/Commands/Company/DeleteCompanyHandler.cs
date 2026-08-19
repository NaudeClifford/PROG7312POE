using SmartX.Shared.Models;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Commands.Sensors;

public class DeleteCompanyHandler
{
    private readonly ISensorRepository _sensorRepository;

    public DeleteCompanyHandler(ISensorRepository sensorRepository)
    {
        _sensorRepository = sensorRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        DeleteCompanyCommand command,
        CancellationToken cancellationToken = default)
    {

        if (command.Id == Guid.Empty)
        {
            return Result<bool>.Fail("Sensor ID is required.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail("Sensor not found.");
        }

        await _sensorRepository.DeleteAsync(
            command.Id,
            cancellationToken);

        return Result<bool>.Ok(true);
    }
}