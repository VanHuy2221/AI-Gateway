using AI_Test.Data;
using AI_Test.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Test.Controllers
{
    [ApiController]
    [Route("usage")]
    [Authorize]
    public class UsageController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsageController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var logs = await _db.RequestLogs.ToListAsync();

            int requests = logs.Count;
            int tokens = logs.Sum(l => l.InputTokens + l.OutputTokens);
            double avgLatency = requests > 0 ? logs.Average(l => l.LatencyMs) : 0;
            double errorRate = requests > 0 ? (double)logs.Count(l => l.Status == "error") / requests : 0;

            return Ok(new UsageResponse(requests, tokens, Math.Round(avgLatency, 2), Math.Round(errorRate, 4)));
        }
    }
}