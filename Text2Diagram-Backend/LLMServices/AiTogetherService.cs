using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.LLMServices;

public class AiTogetherService
{
    private readonly Kernel _kernel;

    public AiTogetherService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<LLMResponse> GenerateContentAsync(string prompt)
    {
        IChatCompletionService chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        return new LLMResponse
        {
            Content = response.Content
        };
    }
}
