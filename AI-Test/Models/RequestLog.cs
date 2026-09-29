namespace AI_Test.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long LatencyMs { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public string Status { get; set; } = string.Empty; // "success" hoặc "error"
        public string? ErrorMessage { get; set; }
    }
}