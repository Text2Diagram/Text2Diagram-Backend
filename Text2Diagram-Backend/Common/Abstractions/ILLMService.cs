namespace Text2Diagram_Backend.Common.Abstractions
{
	public interface ILLMService
	{
		Task<LLMResponse> GenerateContentAsync(string prompt);
	}

	public class LLMRequest
	{
		public string Prompt { get; set; }
	}

	public class LLMResponse
	{
		public string Content { get; set; }
	}
}
