using LangChain.Providers.Ollama;

namespace Text2Diagram_Backend;

public class SequenceDiagramGenerator : IDiagramGenerator
{
    private readonly OllamaProvider provider;
    private readonly IConfiguration configuration;

    public SequenceDiagramGenerator(
        OllamaProvider provider,
        IConfiguration configuration)
    {
        this.provider = provider;
        this.configuration = configuration;
    }

    public async Task<string> GenerateAsync(string input)
    {
        var prompt = GetPrompt();
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        var llm = new OllamaChatModel(provider, id: llmName);

        string result = "";

        return result;
    }


    private string GetPrompt()
    {
        return "sequence diagram of ";
    }
}
