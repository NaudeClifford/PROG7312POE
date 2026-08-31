using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    [Authorize]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]

        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
