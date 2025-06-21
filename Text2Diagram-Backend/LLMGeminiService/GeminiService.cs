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

		public GeminiService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_configuration = configuration;
			_projectId = _configuration["Gemini:ProjectId"];
			_accessToken = _configuration["Gemini:AccessToken"]; // AccessToken lấy từ OAuth Playground hoặc hệ thống của bạn
		}

		public async Task<LLMResponse> GenerateContentAsync(string prompt)
		{
			var endpoint = $"https://us-central1-aiplatform.googleapis.com/v1/projects/{_projectId}/locations/us-central1/publishers/google/models/gemini-1.5-pro:generateContent";

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
