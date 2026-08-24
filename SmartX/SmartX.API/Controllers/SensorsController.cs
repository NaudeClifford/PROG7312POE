using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Commands.Sensors;
using SmartX.Application.Services;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorCrudService _crud;
    private readonly SensorLogFileService _logFiles;
    public SensorsController(
        SensorCrudService crud,
        SensorLogFileService logFiles)
    {
        _crud = crud;
        _logFiles = logFiles;
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
    [HttpGet("{sensorId:guid}/logs")]
    public async Task<IActionResult> GetLogs(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        var result =
            await _logFiles.GetBySensorIdAsync(
                sensorId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("{sensorId:guid}/logs")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadLog(
        Guid userId,
        Guid sensorId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(
                new
                {
                    Success = false,
                    Error = "A file is required."
                });
        }

        if (!string.Equals(
                file.ContentType,
                "text/plain",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                new
                {
                    Success = false,
                    Error = "Only text files are allowed."
                });
        }

        if (!Path.GetExtension(file.FileName)
            .Equals(
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                new
                {
                    Success = false,
                    Error = "Only .txt files are allowed."
                });
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        // Replace this with your actual authenticated
        // SmartX user ID retrieval.
        var uploadedByUserId = userId;

        await using var stream =
            file.OpenReadStream();

        var result =
            await _logFiles.UploadAsync(
                sensorId,
                file.FileName,
                stream,
                "text/plain",
                uploadedByUserId,
                cancellationToken);

        return Ok(new
        {
            Success = true,
            Data = result
        });
    }

}