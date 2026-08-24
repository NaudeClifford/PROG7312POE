using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok"
            });
        }
    }
}
