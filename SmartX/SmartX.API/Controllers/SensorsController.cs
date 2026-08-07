using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Queries.Sensors;
using SmartX.Domain.Entities;

namespace SmartX.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorsController : ControllerBase
    {
        private readonly GetSensorsHandler _getSensorsHandler;
        private readonly GetSensorByIdHandler _getSensorByIdHandler;

        private readonly CreateSensorHandler _createSensorHandler;

        private readonly UpdateSensorHandler _updateSensorHandler;

        private readonly DeleteSensorHandler _deleteSensorHandler;


        public SensorsController(
            GetSensorsHandler getSensorsHandler,
            GetSensorByIdHandler getSensorByIdHandler,
            CreateSensorHandler createSensorHandler,
            UpdateSensorHandler updateSensorHandler,
            DeleteSensorHandler deleteSensorHandler)
        {
            _getSensorsHandler = getSensorsHandler;
            _getSensorByIdHandler = getSensorByIdHandler;
            _createSensorHandler = createSensorHandler;
            _updateSensorHandler = updateSensorHandler;
            _deleteSensorHandler = deleteSensorHandler;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetSensorById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _getSensorByIdHandler.HandleAsync(
                new GetSensorByIdQuery
                {
                    SensorId = id
                }, cancellationToken);

            if (!result.Success) return NotFound(result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetSensors(
            CancellationToken cancellationToken)
        {
            var result = await _getSensorsHandler.HandleAsync(
                new GetSensorsQuery(), cancellationToken);

            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> CreateSensor(
            CreateSensorCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _createSensorHandler.HandleAsync(
                command, cancellationToken);

            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateSensor(
            Guid id,
            [FromBody] UpdateSensorCommand command,
            CancellationToken cancellationToken
            )
        {
            command.Id = id;

            var result = await _updateSensorHandler.HandleAsync(
                command, cancellationToken);

            if (!result.Success) {
                if (result.Error == "Sensor not found.")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteSensor(
            Guid id,
            CancellationToken cancellationToken
            )
        {

            var result = await _deleteSensorHandler.HandleAsync(
                new DeleteSensorCommand 
                {
                    Id = id
                }, cancellationToken);

            if (!result.Success)
            {
                if (result.Error == "Sensor not found.")
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }


    }
}
