namespace Text2Diagram_Backend.LLMGeminiService;

public class GeminiOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string ServiceAccountJsonPath { get; set; } = string.Empty;
}
