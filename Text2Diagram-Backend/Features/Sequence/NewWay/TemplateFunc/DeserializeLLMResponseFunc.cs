using System.Text.Json;

namespace Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc
{
	public static class DeserializeLLMResponseFunc
	{
		public static List<T> DeserializeLLMResponse<T>(string jsonResponse)
		{
			if (string.IsNullOrWhiteSpace(jsonResponse))
				return new List<T>();

			jsonResponse = jsonResponse.Trim();

			try
			{
				// Nếu là array: [ ... ]
				if (jsonResponse.StartsWith("["))
				{
					return JsonSerializer.Deserialize<List<T>>(jsonResponse, new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					}) ?? new List<T>();
				}
				// Nếu là object: { ... }
				else if (jsonResponse.StartsWith("{"))
				{
					var singleItem = JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					});

					return singleItem != null ? new List<T> { singleItem } : new List<T>();
				}
			}
			catch (JsonException ex)
			{
				// Log nếu cần
				Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
			}

			return new List<T>();
		}
	}

}
