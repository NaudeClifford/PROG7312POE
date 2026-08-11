using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Telemetry;
using SmartX.Application.Queries.Telemetry;

namespace SmartX.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            _getTelemetryByDateRangeHandler = getTelemetryByDateRangeHandler;
            _createTelemetryHandler = createTelemetryHandler;
            _getTelemetryByIdHandler = getTelemetryByIdHandler;
            _getTelemetryBySensorHandler = getTelemetryBySensorHandler;
            _getLatestTelemetryBySensorHandler = getLatestTelemetryBySensorHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTelemetry(
            CreateTelemetryCommand command, CancellationToken cancellationToken)
        {
            var result = await _createTelemetryHandler.HandleAsync(
                command, cancellationToken);

            if (!result.Success) return BadRequest(result);

            return Ok(result);

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTelemetryById(
            Guid id, CancellationToken cancellationToken)
        {
            var result = await _getTelemetryByIdHandler.HandleAsync(
                new GetTelemetryByIdQuery
                {
                    TelemetryId = id
                }, cancellationToken);

            if (!result.Success) return NotFound(result);

            return Ok(result);
        }

        [HttpGet("sensor/{sensorId}")]
        public async Task<IActionResult> GetTelemetryBySensor(
            Guid sensorId, CancellationToken cancellationToken) 
        {
            var result = await _getTelemetryBySensorHandler.HandleAsync(
                    new GetTelemetryBySensorQuery
                    {
                        SensorId = sensorId
                    }, cancellationToken);

            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("sensor/{sensorId}/latest")]
        public async Task<IActionResult> GetLatestTelemetryBySensor(
            Guid sensorId, CancellationToken cancellationToken)
        {
            var result = await _getLatestTelemetryBySensorHandler.HandleAsync(
                    new GetLatestTelemetryBySensorQuery
                    {
                        SensorId = sensorId
                    }, cancellationToken);

            if (!result.Success) return NotFound(result);

            return Ok(result);
        }


        [HttpGet("sensor/{sensorId}/history")]
        public async Task<IActionResult> GetTelemetryByDateRange(
            Guid sensorId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            var result = await _getTelemetryByDateRangeHandler.HandleAsync(
                    new GetTelemetryByDateRangeQuery
                    {
                        SensorId = sensorId,
                        From = from,
                        To = to
                    }, cancellationToken);

            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }
    }
}