using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class RejoinPointIdentifier
{
    private readonly ILLMService _llmService;
    private readonly ILogger<RejoinPointIdentifier> _logger;

    public RejoinPointIdentifier(ILLMService llmService, ILogger<RejoinPointIdentifier> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<List<Flow>> AddRejoinPointsAsync(List<Flow> flows)
    {
        var basicFlow = flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow required.");
        var subFlows = flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();
        var modifiedFlows = flows.Select(f => new Flow
        (
            f.Name,
            f.FlowType,
            [.. f.Nodes],
            [.. f.Edges]
        )).ToList();

        foreach (var subFlow in subFlows)
        {
            var endNodes = subFlow.Nodes.Where(n => n.Type == NodeType.End).ToList();
            foreach (var endNode in endNodes)
            {
                var prompt = $"""
                    Given the basic flow nodes: {JsonSerializer.Serialize(basicFlow.Nodes)},
                    and a subFlow named '{subFlow.Name}' with End node label '{endNode.Label}',
                    identify the most likely basic flow node ID where this subFlow rejoins.
                    Return only the node ID or an empty string if no rejoin is applicable.
                    """;
                try
                {
                    var response = await _llmService.GenerateContentAsync(prompt);
                    var rejoinNodeId = response.Content?.Trim() ?? string.Empty;
                    if (basicFlow.Nodes.Any(n => n.Id == rejoinNodeId))
                    {
                        var modifiedSubflow = modifiedFlows.First(f => f.Name == subFlow.Name);
                        modifiedSubflow.Edges.Add(new FlowEdge
                        (
                            $"{subFlow.Name}_{endNode.Id}",
                            rejoinNodeId,
                            EdgeType.Arrow,
                            "Rejoin"
                        ));
                        _logger.LogDebug("Added rejoin edge from {0} to {1} for subFlow {2}.", endNode.Id, rejoinNodeId, subFlow.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to identify rejoin point for subFlow {0}, end node {1}: {2}", subFlow.Name, endNode.Id, ex.Message);
                }
            }
        }

        return modifiedFlows;
    }
}