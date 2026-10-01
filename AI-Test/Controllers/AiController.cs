using System.Security.Claims;
using AI_Test.DTOs;
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
        private readonly ILogger<AiController> _logger;

        public AiController(AiService aiService, ILogger<AiController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null) throw new UnauthorizedAccessException("Không xác định được user.");
            return int.Parse(claim.Value);
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "Message không được để trống." });

            try
            {
                var result = await _aiService.ChatAsync(GetUserId(), request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini /ai/chat");
                return StatusCode(502, new { message = "Không thể gọi Gemini.", error = ex.Message });
            }
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Text không được để trống." });

            try
            {
                var result = await _aiService.AnalyzeAsync(GetUserId(), request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini /ai/analyze");
                return StatusCode(502, new { message = "Không thể phân tích văn bản.", error = ex.Message });
            }
        }
    }
}