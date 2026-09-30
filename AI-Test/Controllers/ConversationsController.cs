using System.Security.Claims;
using AI_Test.Data;
using AI_Test.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Test.Controllers
{
    [ApiController]
    [Route("conversations")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ConversationsController(AppDbContext db) { _db = db; }

        private int GetUserId() =>
            int.Parse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))!.Value);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var conversations = await _db.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.CreatedAt, c.Messages.Count))
                .ToListAsync();

            return Ok(conversations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var conversation = await _db.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (conversation == null) return NotFound();

            var dto = new ConversationDetailDto(
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt,
                conversation.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageDto(m.Role, m.Content, m.CreatedAt))
                    .ToList()
            );

            return Ok(dto);
        }
    }
}