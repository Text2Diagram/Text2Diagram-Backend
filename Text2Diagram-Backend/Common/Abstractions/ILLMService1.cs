namespace Text2Diagram_Backend.Common.Abstractions
{
    public interface ILLMService1
    {
        Task<LLMResponse> GenerateContentAsync(string prompt);
    }

    public interface ILLMService2
    {
        Task<LLMResponse> GenerateContentAsync(string prompt);
    }

    public interface ILLMService3
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
