using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestAuthController : ControllerBase
{
    [HttpGet("auth")]
    [Authorize]
    public IActionResult TestAuthentication()
    {
        return Ok(new
        {
            Message = "Firebase authentication successful.",
            UserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        });
    }
}