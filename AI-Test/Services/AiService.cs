using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AI_Test.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> ChatAsync(string message)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("GEMINI_API_KEY chưa được cấu hình.");
            }

            var url =
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = message
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("x-goog-api-key", apiKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Gemini API lỗi: {response.StatusCode} - {responseContent}"
                );
            }

            using var document = JsonDocument.Parse(responseContent);

            var text = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "";
        }
    }
}