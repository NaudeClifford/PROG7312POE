using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, Technician")]

public class SensorsController : ControllerBase
{
    private readonly SensorCrudService _crud;

    public SensorsController(
        SensorCrudService crud)
    {
        _crud = crud;
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetAllAsync(
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // GET BY ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetByIdAsync(
                id,
                cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    // CREATE
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSensorRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.CreateAsync(
                request,
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // UPDATE

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSensorRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var result =
            await _crud.UpdateAsync(
                request,
                cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "Sensor not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    // DELETE

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.DeleteAsync(
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
