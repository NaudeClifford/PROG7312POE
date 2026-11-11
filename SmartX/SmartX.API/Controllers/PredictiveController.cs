
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Services.PredictiveEngine;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using System.Security.Claims;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PredictiveController : ControllerBase
{
    private readonly IPredictiveSuggestionProvider _suggestionProvider;
    private readonly IAnomalyRecommendationService _anomalyService;
    private readonly IPredictiveWarningService _warningService;
    private readonly IUserRepository _userRepository;

    public PredictiveController(
        IPredictiveSuggestionProvider suggestionProvider,
        IAnomalyRecommendationService anomalyService,
        IPredictiveWarningService warningService,
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _suggestionProvider = suggestionProvider;
        _anomalyService = anomalyService;
        _warningService = warningService;
    }



    [HttpGet("sensors/{sensorId:guid}/suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<PredictiveSuggestionDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<IReadOnlyList<PredictiveSuggestionDto>>>
        GetSuggestions(
            Guid sensorId,
            CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Invalid sensor ID.");

        var firebaseUid =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(firebaseUid))
            return Unauthorized();

        var user = await _userRepository.GetByFirebaseUidAsync(
            firebaseUid,
            cancellationToken);

        if (user is null)
            return Unauthorized();

        var suggestions =
            await _suggestionProvider.GetSuggestionsAsync(
                sensorId,
                user.Id,
                cancellationToken);

        return Ok(suggestions);
    }



    [HttpGet("sensors/{sensorId:guid}/anomaly")]
    [ProducesResponseType(typeof(AnomalyRecommendationDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AnomalyRecommendationDto>>
        AnalyzeAnomaly(
            Guid sensorId,
            CancellationToken cancellationToken)
    {
        if (sensorId == Guid.Empty)
            return BadRequest("Invalid sensor ID.");

        var recommendation =
            await _anomalyService.AnalyzeAsync(
                sensorId,
                cancellationToken);

        if (recommendation is null)
        {
            return NotFound(
                "No anomaly detected or insufficient telemetry.");
        }

        return Ok(recommendation);
    }

    [HttpGet("gateway/{gatewayId:guid}/warnings")]
    public async Task<ActionResult<IReadOnlyList<PredictiveSuggestionDto>>>
    GetWarnings(
        Guid gatewayId,
        CancellationToken cancellationToken)
    {
        if (gatewayId == Guid.Empty)
            return BadRequest("Invalid gateway ID.");

        var firebaseUid =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(firebaseUid))
            return Unauthorized();

        // Resolve Firebase UID to your internal application user ID.
        var user = await _userRepository.GetByFirebaseUidAsync(
            firebaseUid,
            cancellationToken);

        if (user is null)
            return Unauthorized();

        var warnings =
            await _warningService.GetWarningsAsync(
                gatewayId,
                user.Id,
                cancellationToken);

        return Ok(warnings);
    }
}
