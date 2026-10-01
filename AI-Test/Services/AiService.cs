using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AI_Test.Data;
using AI_Test.DTOs;
using AI_Test.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Test.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<AiService> _logger;
        private readonly string _model;

        public AiService(HttpClient httpClient, IConfiguration config, AppDbContext db, ILogger<AiService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _db = db;
            _logger = logger;
            _model = _config["Gemini:Model"] ?? "gemini-2.5-flash";
        }

        public async Task<ChatResponse> ChatAsync(int userId, ChatRequest request)
        {
            var conversation = await GetOrCreateConversationAsync(userId, request.ConversationId, request.Message);

            // Lấy lịch sử hội thoại TRƯỚC KHI thêm tin nhắn mới, để build ngữ cảnh gửi cho Gemini
            var priorMessages = await _db.Messages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            _db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = request.Message
            });
            await _db.SaveChangesAsync();

            var stopwatch = Stopwatch.StartNew();
            string status = "success";
            string? errorMessage = null;
            int inputTokens = 0, outputTokens = 0;
            string replyText = "";

            try
            {
                // Chỉ giữ tối đa 20 tin nhắn gần nhất để tránh tốn quá nhiều token/quota
                var historyForGemini = priorMessages.TakeLast(20).ToList();
                var contents = BuildContents(historyForGemini, request.Message);

                var (text, promptTokens, candidateTokens) = await CallGeminiAsync(contents, structuredJson: false);
                replyText = text;
                inputTokens = promptTokens;
                outputTokens = candidateTokens;
            }
            catch (Exception ex)
            {
                status = "error";
                errorMessage = ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await LogRequestAsync(userId, stopwatch.ElapsedMilliseconds, inputTokens, outputTokens, status, errorMessage);
            }

            _db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = replyText
            });
            await _db.SaveChangesAsync();

            return new ChatResponse(conversation.Id, replyText, _model, stopwatch.ElapsedMilliseconds, inputTokens, outputTokens);
        }

        // Chuyển lịch sử hội thoại (đã lưu trong DB) sang định dạng "contents" mà Gemini yêu cầu.
        // Gemini dùng role "user" và "model" (không phải "assistant").
        private static object[] BuildContents(List<Message> priorMessages, string newUserMessage)
        {
            var contents = new List<object>();

            foreach (var m in priorMessages)
            {
                contents.Add(new
                {
                    role = m.Role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = m.Content } }
                });
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = newUserMessage } }
            });

            return contents.ToArray();
        }

        public async Task<AnalyzeResponse> AnalyzeAsync(int userId, AnalyzeRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            string status = "success";
            string? errorMessage = null;
            int inputTokens = 0, outputTokens = 0;

            try
            {
                var prompt = $"Phân tích đoạn văn bản sau và trả lời đúng theo schema.\n\nVăn bản: \"{request.Text}\"";

                var contents = new object[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                };
                var (text, promptTokens, candidateTokens) = await CallGeminiAsync(contents, structuredJson: true);
                inputTokens = promptTokens;
                outputTokens = candidateTokens;

                var result = JsonSerializer.Deserialize<AnalyzeResponse>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new AnalyzeResponse("Không phân tích được.", "unknown", new List<string>());
            }
            catch (Exception ex)
            {
                status = "error";
                errorMessage = ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                await LogRequestAsync(userId, stopwatch.ElapsedMilliseconds, inputTokens, outputTokens, status, errorMessage);
            }
        }

        // Gọi Gemini kèm retry (tối đa 3 lần) và timeout 20s mỗi lần gọi.
        // Retry chỉ áp dụng cho lỗi tạm thời: HTTP 429, 5xx, timeout, lỗi mạng.
        private async Task<(string text, int inputTokens, int outputTokens)> CallGeminiAsync(object[] contents, bool structuredJson)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("GEMINI_API_KEY chưa được cấu hình.");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

            object requestBody = structuredJson
                ? new
                {
                    contents = contents,
                    generationConfig = new
                    {
                        responseMimeType = "application/json",
                        responseSchema = new
                        {
                            type = "OBJECT",
                            properties = new
                            {
                                summary = new { type = "STRING" },
                                sentiment = new { type = "STRING" },
                                keywords = new { type = "ARRAY", items = new { type = "STRING" } }
                            },
                            required = new[] { "summary", "sentiment", "keywords" }
                        }
                    }
                }
                : new { contents = contents };

            var json = JsonSerializer.Serialize(requestBody);
            const int maxAttempts = 3;
            Exception? lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("x-goog-api-key", apiKey);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        bool transient = (int)response.StatusCode == 429 || (int)response.StatusCode >= 500;
                        var error = new Exception($"Gemini API lỗi: {response.StatusCode} - {responseContent}");

                        if (transient && attempt < maxAttempts)
                        {
                            _logger.LogWarning("Gemini trả lỗi tạm thời (lần {Attempt}): {Status}", attempt, response.StatusCode);
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                            lastError = error;
                            continue;
                        }
                        throw error;
                    }

                    using var document = JsonDocument.Parse(responseContent);
                    var root = document.RootElement;

                    var text = root.GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "";

                    int inputTokens = 0, outputTokens = 0;
                    if (root.TryGetProperty("usageMetadata", out var usage))
                    {
                        if (usage.TryGetProperty("promptTokenCount", out var p)) inputTokens = p.GetInt32();
                        if (usage.TryGetProperty("candidatesTokenCount", out var c)) outputTokens = c.GetInt32();
                    }

                    return (text, inputTokens, outputTokens);
                }
                catch (TaskCanceledException ex)
                {
                    lastError = new Exception("Gemini API timeout sau 20 giây.", ex);
                    _logger.LogWarning("Gemini timeout (lần {Attempt})", attempt);
                    if (attempt < maxAttempts)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
                catch (HttpRequestException ex)
                {
                    lastError = ex;
                    _logger.LogWarning("Lỗi mạng khi gọi Gemini (lần {Attempt}): {Message}", attempt, ex.Message);
                    if (attempt < maxAttempts)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }

            throw lastError ?? new Exception("Không gọi được Gemini API sau nhiều lần thử.");
        }

        private async Task<Conversation> GetOrCreateConversationAsync(int userId, int? conversationId, string firstMessage)
        {
            if (conversationId.HasValue)
            {
                var existing = await _db.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.UserId == userId);
                if (existing != null) return existing;
            }

            var title = firstMessage.Length > 50 ? firstMessage[..50] + "..." : firstMessage;
            var conversation = new Conversation { UserId = userId, Title = title };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
            return conversation;
        }

        private async Task LogRequestAsync(int userId, long latencyMs, int inputTokens, int outputTokens, string status, string? errorMessage)
        {
            _db.RequestLogs.Add(new RequestLog
            {
                UserId = userId,
                Model = _model,
                LatencyMs = latencyMs,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                Status = status,
                ErrorMessage = errorMessage
            });
            await _db.SaveChangesAsync();
        }
    }
}