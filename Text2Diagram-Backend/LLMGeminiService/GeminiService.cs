using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.LLMGeminiService
{
    public class GeminiService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _projectId;
        private readonly string _accessToken;
        private readonly string _modelId;
        private readonly string _region;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _projectId = _configuration["Gemini:ProjectId"] ?? throw new ArgumentException("Gemini:ProjectId was not defined.");
            _accessToken = _configuration["Gemini:AccessToken"] ?? throw new ArgumentException("Gemini:AccessToken was not defined.");
            _modelId = _configuration["Gemini:ModelId"] ?? throw new ArgumentException("Gemini:ModelId was not defined.");
            _region = _configuration["Gemini:Region"] ?? throw new ArgumentException("Gemini:Region was not defined.");
        }

        public async Task<LLMResponse> GenerateContentAsync(string prompt)
        {
            var endpoint = $"https://us-central1-aiplatform.googleapis.com/v1/projects/{_projectId}/locations/{_region}/publishers/google/models/{_modelId}:generateContent";

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
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
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
                Content = text
            };
        }
    }

}
