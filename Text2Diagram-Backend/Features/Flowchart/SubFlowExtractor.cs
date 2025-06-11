using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Text2Diagram_Backend.Features.Flowchart.Components;
using Text2Diagram_Backend.Common.Utils;

namespace Text2Diagram_Backend.Features.Flowchart;

public class SubFlowExtractor
{
    private readonly Kernel _kernel;
    private readonly ILogger<SubFlowExtractor> _logger;
    private IChatCompletionService _chatCompletionService;

    public SubFlowExtractor(
        Kernel kernel,
        ILogger<SubFlowExtractor> logger)
    {
        _kernel = kernel;
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    public async Task<SubFlow> ExtractAlternativeFlowsAsync(string flowDescription)
    {
        var name = "";
        var nodes = await ExtractNodesForAlternativeFlowAsync(flowDescription);
        var edges = await ExtractEdgesForAlternativeFlowAsync(nodes);
        return new SubFlow(name, nodes, edges);
    }

    public async Task<SubFlow> ExtractExceptionFlowsAsync(string flowDescription)
    {
        var name = "";
        var nodes = await ExtractNodesForExceptionFlowAsync(flowDescription);
        var edges = await ExtractEdgesForExceptionFlowAsync(nodes);
        return new SubFlow(name, nodes, edges);
    }

    private async Task<List<FlowNode>> ExtractNodesForAlternativeFlowAsync(string flowDescription)
    {
        var prompt = $"";
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;

        var nodes = Helpers.Flowchart.ExtractNodes(textContent);
        return nodes;
    }

    private async Task<List<FlowEdge>> ExtractEdgesForAlternativeFlowAsync(List<FlowNode> nodes)
    {
        var prompt = $"";
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;
        var edges = Helpers.Flowchart.ExtractEdges(textContent);

        return edges;
    }

    private async Task<List<FlowNode>> ExtractNodesForExceptionFlowAsync(string flowDescription)
    {
        var prompt = $"";
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;

        var nodes = Helpers.Flowchart.ExtractNodes(textContent);
        return nodes;
    }

    private async Task<List<FlowEdge>> ExtractEdgesForExceptionFlowAsync(List<FlowNode> nodes)
    {
        var prompt = $"";
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;
        var edges = Helpers.Flowchart.ExtractEdges(textContent);

        return edges;
    }
}
