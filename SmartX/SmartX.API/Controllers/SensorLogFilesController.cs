using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.SensorLogFile;
using SmartX.Application.Services.CRUD;
using System.Security.Claims;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator, Technician")]
public class SensorLogFilesController : ControllerBase
{
    private readonly SensorLogFileCrudService _crud;

    public SensorLogFilesController(
        SensorLogFileCrudService crud)
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

    // GET BY SENSOR

    [HttpGet("sensor/{sensorId:guid}")]
    public async Task<IActionResult> GetBySensorId(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        var result =
            await _crud.GetBySensorIdAsync(
                sensorId,
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // UPLOAD

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Create(
        [FromForm] CreateSensorLogFileRequest request,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var userIdClaim =
            User.FindFirst("smartx_user_id")?.Value
            ?? User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                userIdClaim,
                out var uploadedByUserId) ||
            uploadedByUserId == Guid.Empty)
        {
            return Unauthorized(
                new
                {
                    Success = false,
                    Error =
                        "Authenticated SmartX user ID could not be determined."
                });
        }

        var result =
            await _crud.CreateAsync(
                request,
                uploadedByUserId,
                cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // DELETE

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]

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
            if (result.Error ==
                "Sensor log file not found.")
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }
}
