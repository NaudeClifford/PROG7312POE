
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Services.Alerts;
using SmartX.Shared.DTOs;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AlertController : ControllerBase
{
    private readonly IAlertQueue _alertQueue;

    public AlertController(IAlertQueue alertQueue)
    {
        _alertQueue = alertQueue;
    }

    // GET: api/alert
    [HttpGet]
    public ActionResult<IReadOnlyList<SensorAlert>> GetAlerts()
    {
        var alerts = _alertQueue.GetAlerts().ToList();

        return Ok(alerts);
    }

    // GET: api/alert/count
    [HttpGet("count")]
    public ActionResult<int> GetPendingCount()
    {
        return Ok(_alertQueue.Count);
    }

    // POST: api/alert/process-next
    [HttpPost("process-next")]
    public ActionResult<SensorAlert> ProcessNext()
    {
        if (!_alertQueue.TryDequeue(out var alert) ||
            alert is null)
        {
            return NoContent();
        }

        return Ok(alert);
    }

    // POST: api/alert/process-all
    [HttpPost("process-all")]
    public ActionResult<IReadOnlyList<SensorAlert>> ProcessAll()
    {
        var processed = new List<SensorAlert>();

        while (_alertQueue.TryDequeue(out var alert))
        {
            if (alert is not null)
                processed.Add(alert);
        }

        return Ok(processed);
    }

    // DELETE: api/alert
    [HttpDelete]
    public IActionResult ClearAlerts()
    {
        _alertQueue.Clear();

        return NoContent();
    }

    // POST: api/alert
    [HttpPost]
    public IActionResult EnqueueAlert([FromBody] SensorAlert alert)
    {

        if (alert.SensorId == Guid.Empty)
            return BadRequest("A valid sensor ID is required.");

        if (string.IsNullOrWhiteSpace(alert.Message))
            return BadRequest("An alert message is required.");

        _alertQueue.Enqueue(alert);

        return Accepted(alert);
    }
}
