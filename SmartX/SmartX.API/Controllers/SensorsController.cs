using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.Sensor;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, Technician, SuperAdmin")]
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

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // GET BY ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        var result =
            await _crud.GetByIdAsync(
                id,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Sensor not found."
            ? NotFound(result)
            : Forbid();
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
                User,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }
    // UPDATE
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSensorRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        request.Id = id;

        var result =
            await _crud.UpdateAsync(
                request,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Sensor not found."
            ? NotFound(result)
            : result.Error == "You do not have access to this sensor."
                ? Forbid()
                : BadRequest(result);
    }

    // DELETE
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest("Sensor ID is required.");

        var result =
            await _crud.DeleteAsync(
                id,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error == "Sensor not found."
            ? NotFound(result)
            : result.Error == "You do not have access to this sensor."
                ? Forbid()
                : BadRequest(result);
    }

    [HttpGet("gateway/{gatewayId:guid}")]
    public async Task<IActionResult> GetByGatewayId(
    Guid gatewayId,
    CancellationToken cancellationToken)
    {
        if (gatewayId == Guid.Empty)
            return BadRequest("Gateway ID is required.");

        var result =
            await _crud.GetByGatewayIdAsync(
                gatewayId,
                User,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.Error switch
        {
            "Gateway not found." =>
                NotFound(result),

            "You do not have access to this gateway." =>
                Forbid(),

            _ =>
                BadRequest(result)
        };
    }

}