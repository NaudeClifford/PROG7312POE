using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorCrudService _crud;

    public SensorsController(
        SensorCrudService crud)
    {
        _crud = crud;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetAllAsync(
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.GetByIdAsync(
            id,
            cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSensorCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _crud.CreateAsync(
            command,
            cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSensorCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;

        var result = await _crud.UpdateAsync(
            command,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Sensor not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _crud.DeleteAsync(
            id,
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Sensor not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}