namespace AI_Test.DTOs
{
    public record ChatRequest(string Message, int? ConversationId);
    public record ChatResponse(int ConversationId, string Reply, string Model, long LatencyMs, int InputTokens, int OutputTokens);

    public record AnalyzeRequest(string Text);
    public record AnalyzeResponse(string Summary, string Sentiment, List<string> Keywords);

    public record UsageResponse(int Requests, int Tokens, double AverageLatencyMs, double ErrorRate);

    public record ConversationSummaryDto(int Id, string Title, DateTime CreatedAt, int MessageCount);
    public record MessageDto(string Role, string Content, DateTime CreatedAt);
    public record ConversationDetailDto(int Id, string Title, DateTime CreatedAt, List<MessageDto> Messages);
}