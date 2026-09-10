using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Queries.Telemetry;
using SmartX.Application.Requests.Telemetry;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, Technician")]
public class TelemetryController : ControllerBase
{
    private readonly CreateTelemetryHandler _createTelemetryHandler;
    private readonly GetTelemetryByIdHandler _getTelemetryByIdHandler;
    private readonly GetTelemetryBySensorHandler _getTelemetryBySensorHandler;
    private readonly GetLatestTelemetryBySensorHandler _getLatestTelemetryBySensorHandler;
    private readonly GetTelemetryByDateRangeHandler _getTelemetryByDateRangeHandler;

    public TelemetryController(
        CreateTelemetryHandler createTelemetryHandler,
        GetTelemetryByIdHandler getTelemetryByIdHandler,
        GetTelemetryBySensorHandler getTelemetryBySensorHandler,
        GetLatestTelemetryBySensorHandler getLatestTelemetryBySensorHandler,
        GetTelemetryByDateRangeHandler getTelemetryByDateRangeHandler)
    {
        _createTelemetryHandler = createTelemetryHandler;
        _getTelemetryByIdHandler = getTelemetryByIdHandler;
        _getTelemetryBySensorHandler = getTelemetryBySensorHandler;
        _getLatestTelemetryBySensorHandler =
            getLatestTelemetryBySensorHandler;
        _getTelemetryByDateRangeHandler =
            getTelemetryByDateRangeHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTelemetry(
        CreateTelemetryRequest command,
        CancellationToken cancellationToken)
    {
        var result =
            await _createTelemetryHandler.HandleAsync(
                command,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTelemetryById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Telemetry ID is required.");

        var result =
            await _getTelemetryByIdHandler.HandleAsync(
                new GetTelemetryByIdQuery
                {
                    TelemetryId = id
                }, User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Telemetry not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpGet("sensor/{sensorId:guid}")]
    public async Task<IActionResult> GetTelemetryBySensor(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        var result =
            await _getTelemetryBySensorHandler.HandleAsync(
                new GetTelemetryBySensorQuery
                {
                    SensorId = sensorId
                }, User ,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    [HttpGet("sensor/{sensorId:guid}/latest")]
    public async Task<IActionResult> GetLatestTelemetryBySensor(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        var result =
            await _getLatestTelemetryBySensorHandler.HandleAsync(
                new GetLatestTelemetryBySensorQuery
                {
                    SensorId = sensorId
                },
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Telemetry not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    [HttpGet("sensor/{sensorId:guid}/history")]
    public async Task<IActionResult> GetTelemetryByDateRange(
        Guid sensorId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        if (from > to)
            return BadRequest(
                "The 'from' date must be before the 'to' date.");

        var result =
            await _getTelemetryByDateRangeHandler.HandleAsync(
                new GetTelemetryByDateRangeQuery
                {
                    SensorId = sensorId,
                    From = from,
                    To = to
                },
                User,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }
}
