using Microsoft.AspNetCore.Mvc;

namespace AI_Test.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "ok", timeUtc = DateTime.UtcNow });
        }
    }
}