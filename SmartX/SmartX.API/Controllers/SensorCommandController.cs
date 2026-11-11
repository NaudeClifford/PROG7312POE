
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Services.Sensors.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SensorCommandController : ControllerBase
{
    private readonly ISensorCommandService _sensorCommandService;

    public SensorCommandController(
        ISensorCommandService sensorCommandService)
    {
        _sensorCommandService = sensorCommandService;
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(SensorCommandResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SensorCommandResultDto>> SendCommand(
        [FromBody] SendSensorCommandDto request,
        CancellationToken cancellationToken)
    {
        if (request.SensorId == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest("Command is required.");

        var result = await _sensorCommandService.SendCommandAsync(
            request.SensorId,
            request.Command,
            cancellationToken);

        return Ok(new SensorCommandResultDto
        {
            Success = result.Success,
            Response = result.Response,
            ErrorMessage = result.ErrorMessage
        });
    }

    [HttpPost("{sensorId:guid}/undo")]
    [ProducesResponseType(typeof(SensorCommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SensorCommandResultDto>> UndoLastCommand(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        var result = await _sensorCommandService.UndoLastCommandAsync(
            sensorId,
            cancellationToken);

        return Ok(new SensorCommandResultDto
        {
            Success = result.Success,
            Response = result.Response,
            ErrorMessage = result.ErrorMessage
        });
    }
}
