using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart;
using Text2Diagram_Backend.Features.Flowchart.Components;

public class DecisionNodeInserter
{
    private readonly ILLMService _llmService;
    private readonly ILogger<DecisionNodeInserter> _logger;

    public DecisionNodeInserter(ILLMService llmService, ILogger<DecisionNodeInserter> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<(List<Flow> Flows, List<(string SubflowName, string BranchNodeId)> BranchingPoints)>
        InsertDecisionNodesAsync(List<Flow> flows)
    {
        var branchingPoints = new List<(string SubflowName, string BranchNodeId)>();
        var basicFlow = flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow required.");
        var subflows = flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();
        var modifiedBasicFlow = new Flow
        (
            basicFlow.Name,
            basicFlow.FlowType,
            [.. basicFlow.Nodes],
            [.. basicFlow.Edges]
        );

        foreach (var subflow in subflows)
        {
            var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
            if (startNode == null)
            {
                _logger.LogWarning("Subflow {0} has no Start node.", subflow.Name);
                continue;
            }

            try
            {
                var prompt = $"""
                    Given the basic flow nodes: {JsonSerializer.Serialize(basicFlow.Nodes.Select(n => new { n.Id, n.Label, n.Type }))},
                    and a subflow named '{subflow.Name}' (Type: {subflow.FlowType}) with Start node label '{startNode.Label}',
                    generate a decision question (e.g., 'Is transfer scheduled?') for branching to this subflow in the 'Transfer Money' use case.
                    """
                    +
                    """
                    Return JSON: { "DecisionLabel": "", "BranchNodeId": "" }
                    """;
                var response = await _llmService.GenerateContentAsync(prompt);
                var res = JsonSerializer.Deserialize<Dictionary<string, string>>(response.Content);
                var decisionLabel = res?["DecisionLabel"] ?? $"Branch to {subflow.Name}?";
                var branchNodeId = res?["BranchNodeId"] ?? string.Empty;

                var decisionNodeId = $"decision_{subflow.Name}";
                var decisionNode = new FlowNode(decisionNodeId, decisionLabel, NodeType.Decision);

                if (string.IsNullOrEmpty(branchNodeId) || !basicFlow.Nodes.Any(n => n.Id == branchNodeId))
                {
                    _logger.LogWarning("Invalid or missing branch node ID for subflow {0}, using decision node as branch.", subflow.Name);
                    branchNodeId = decisionNodeId;
                }

                modifiedBasicFlow.Nodes.Add(decisionNode);
                var branchEdge = modifiedBasicFlow.Edges.FirstOrDefault(e => e.TargetId == branchNodeId);
                if (branchEdge != null)
                {
                    branchEdge.TargetId = decisionNodeId;
                    modifiedBasicFlow.Edges.Add(new FlowEdge
                    (
                        decisionNodeId,
                        branchNodeId,
                        EdgeType.Arrow,
                        "No"
                    ));
                    modifiedBasicFlow.Edges.Add(new FlowEdge
                    (
                        decisionNodeId,
                        $"{subflow.Name}_start_1",
                        subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : EdgeType.Arrow,
                        ""
                    ));
                }

                branchingPoints.Add((subflow.Name, decisionNodeId));
                _logger.LogDebug("Added decision node {0} for subflow {1}, branching from {2}.", decisionNodeId, subflow.Name, branchNodeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to insert decision node for subflow {0}: {1}", subflow.Name, ex.Message);
            }
        }

        var result = new List<Flow> { modifiedBasicFlow };
        result.AddRange(subflows);
        return (result, branchingPoints);
    }
}