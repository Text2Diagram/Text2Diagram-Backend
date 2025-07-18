﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc
{
	public static class DeserializeLLMResponseFunc
	{
		public static List<T> DeserializeLLMResponse<T>(string jsonResponse)
		{
			if (string.IsNullOrWhiteSpace(jsonResponse))
				return new List<T>();

			jsonResponse = jsonResponse.Trim();

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
			};

			try
			{
				// Nếu là array: [ ... ]
				if (jsonResponse.StartsWith("["))
				{
					return JsonSerializer.Deserialize<List<T>>(jsonResponse, options) ?? new List<T>();
				}
				// Nếu là object: { ... }
				else if (jsonResponse.StartsWith("{"))
				{
					var singleItem = JsonSerializer.Deserialize<T>(jsonResponse, options);
					return singleItem != null ? new List<T> { singleItem } : new List<T>();
				}
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
			}

			return new List<T>();
		}

	}

}
