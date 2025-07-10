using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.LLMGeminiService
{
    public class GeminiService3 : ILLMService3
    {
        private readonly GeminiOptions _options;
        private readonly HttpClient _httpClient;

        public GeminiService3(
            IOptions<GeminiOptions> options,
            HttpClient httpClient)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(_options.ProjectId) || string.IsNullOrEmpty(_options.Region) || string.IsNullOrEmpty(_options.ModelId))
                throw new ArgumentException("GeminiOptions must have valid ProjectId, Region, and ModelId.");

            _httpClient = httpClient;
        }

        public async Task<LLMResponse> GenerateContentAsync(string prompt)
        {
            var endpoint = $"https://us-central1-aiplatform.googleapis.com/v1/projects/{_options.ProjectId}/locations/{_options.Region}/publishers/google/models/{_options.ModelId}:generateContent";

            var requestBody = new
            {
                contents = new[]
                {
                new {
                    role = "user",
                    parts = new[] {
                        new { text = prompt }
                    }
                }
            }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API failed: {error}");
            }

            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return new LLMResponse
            {
                Content = text ?? string.Empty
            };
        }
    }

}
