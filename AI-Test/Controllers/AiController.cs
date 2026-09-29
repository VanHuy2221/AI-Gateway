using AI_Test.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI_Test.Controllers
{
    [ApiController]
    [Route("ai")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly AiService _aiService;

        public AiController(AiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new
                {
                    message = "Message không được để trống."
                });
            }

            try
            {
                var result = await _aiService.ChatAsync(request.Message);

                return Ok(new
                {
                    message = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Không thể gọi Gemini.",
                    error = ex.Message
                });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }
}