using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartX.Application.Requests.UserBehavior;
using SmartX.Application.Services.CRUD;

namespace SmartX.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class UserBehaviorController : ControllerBase
    {
        private readonly UserBehaviorCrudService _crud;

        public UserBehaviorController(UserBehaviorCrudService crud)
        {
            _crud = crud;
        }

        // GET: api/UserBehavior
        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _crud.GetAllAsync(cancellationToken);

            return result.Success
                ? Ok(result)
                : BadRequest(result.Error);
        }

        // GET: api/UserBehavior/sensor/{sensorId}
        [HttpGet("sensor/{sensorId:guid}")]
        public async Task<IActionResult> GetBySensorId(
            [FromRoute] Guid sensorId,
            CancellationToken cancellationToken)
        {
            var result = await _crud.GetBySensorId(
                sensorId,
                User,
                cancellationToken);

            if (result.Success)
                return Ok(result);

            if (IsAccessError(result.Error))
                return Forbid();

            if (result.Error == "Sensor ID is required.")
                return BadRequest(result.Error);

            return BadRequest(result.Error);
        }

        // GET: api/UserBehavior/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _crud.GetByIdAsync(
                id,
                User,
                cancellationToken);

            if (result.Success)
                return Ok(result);

            if (IsNotFoundError(result.Error))
                return NotFound(result.Error);

            if (IsAccessError(result.Error))
                return Forbid();

            return BadRequest(result.Error);
        }

        // POST: api/UserBehavior
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserBehaviorRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _crud.CreateAsync(
                request,
                User,
                cancellationToken);

            if (result.Success)
            {
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data },
                    result);
            }

            if (IsAccessError(result.Error))
                return Forbid();

            if (result.Error == "Sensor not found.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        // PUT: api/UserBehavior/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateUserBehaviorRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
                return BadRequest("Request is required.");

            // Prevent the URL ID and request ID from disagreeing.
            if (id == Guid.Empty || request.Id != id)
            {
                return BadRequest(
                    "The route ID must match the request ID.");
            }

            var result = await _crud.UpdateAsync(
                request,
                User,
                cancellationToken);

            if (result.Success)
                return Ok(result);

            if (IsNotFoundError(result.Error))
                return NotFound(result.Error);

            if (IsAccessError(result.Error))
                return Forbid();

            return BadRequest(result.Error);
        }

        // DELETE: api/UserBehavior/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _crud.DeleteAsync(
                id,
                User,
                cancellationToken);

            if (result.Success)
                return Ok(result);

            if (IsNotFoundError(result.Error))
                return NotFound(result.Error);

            if (IsAccessError(result.Error))
                return Forbid();

            return BadRequest(result.Error);
        }

        private static bool IsNotFoundError(string? error)
        {
            return error is not null &&
                (error.Equals(
                    "User behavior not found.",
                    StringComparison.OrdinalIgnoreCase) ||
                 error.Equals(
                    "Sensor not found.",
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAccessError(string? error)
        {
            return error is not null &&
                (error.Contains(
                    "access",
                    StringComparison.OrdinalIgnoreCase) ||
                 error.Contains(
                    "permission",
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}