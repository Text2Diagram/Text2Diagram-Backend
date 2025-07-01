using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class DecisionNodeInserter
{
    private readonly ILLMService _llmService;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly ILogger<DecisionNodeInserter> _logger;

    public DecisionNodeInserter(
        ILLMService llmService,
        IHubContext<ThoughtProcessHub> hubContext,
        ILogger<DecisionNodeInserter> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _hubContext = hubContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(List<Flow> Flows, List<BranchingPoint> BranchingPoints)>
        InsertDecisionNodesAsync(List<Flow> flows, string useCaseDomain = "general")
    {
        var branchingPoints = new List<BranchingPoint>();
        var basicFlow = flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow is required.");
        var subflows = flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();
        var modifiedBasicFlow = new Flow
        (
            string.IsNullOrEmpty(basicFlow.Name) ? "BasicFlow" : basicFlow.Name,
            basicFlow.FlowType,
            [.. basicFlow.Nodes],
            [.. basicFlow.Edges]
        );

        var sw = Stopwatch.StartNew();
        await _hubContext.Clients.All.SendAsync("DetermineInsertionPointsStep", "Determine insertion points...");

        foreach (var subflow in subflows)
        {
            var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
            if (startNode == null)
            {
                _logger.LogWarning("Subflow {SubflowName} is missing a start node. Skipping.", subflow.Name);
                continue;
            }

            var targetNode = subflow.Edges
                    .Where(e => e.SourceId == startNode.Id)
                    .Select(e => subflow.Nodes.FirstOrDefault(n => n.Id == e.TargetId))
                    .FirstOrDefault();

            if (targetNode == null)
            {
                _logger.LogWarning("Subflow {SubflowName} has no valid non-decision entry node. Skipping.", subflow.Name);
                continue;
            }

            var decisionNodeId = $"decision_{subflow.Name}";
            var targetId = $"{subflow.Name}_{targetNode.Id}";

            // Determine insertion point 
            var insertionNodeId = await DetermineInsertionPointAsync(modifiedBasicFlow, subflow, targetNode, useCaseDomain);

            // Generate decision label
            var decisionLabel = await GenerateDecisionLabelAsync(subflow, targetNode, useCaseDomain);

            // Insert decision node if not already present
            if (!modifiedBasicFlow.Nodes.Any(n => n.Id == decisionNodeId))
            {
                modifiedBasicFlow.Nodes.Add(new FlowNode(decisionNodeId, decisionLabel, NodeType.Decision));
            }

            // Set edge labels based on subflow type
            string basicFlowLabel = subflow.FlowType == FlowType.Exception ? "Yes" : "No";
            string subflowLabel = subflow.FlowType == FlowType.Exception ? "No" : "Yes";

            // Update edges
            var insertionEdge = modifiedBasicFlow.Edges.FirstOrDefault(e => e.TargetId == insertionNodeId);
            if (insertionEdge != null)
            {
                insertionEdge.TargetId = decisionNodeId;
                modifiedBasicFlow.Edges.Add(new FlowEdge(
                    decisionNodeId,
                    insertionNodeId,
                    EdgeType.Arrow,
                    basicFlowLabel
                ));
                modifiedBasicFlow.Edges.Add(new FlowEdge(
                    decisionNodeId,
                    targetId,
                    subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : EdgeType.Arrow,
                    subflowLabel
                ));
            }
            else
            {
                _logger.LogWarning("No insertion edge found for node {InsertionNodeId} in subflow {SubflowName}. Skipping.",
                    insertionNodeId, subflow.Name);
                continue;
            }

            branchingPoints.Add(new BranchingPoint(subflow.Name, decisionNodeId));
            _logger.LogInformation("Inserted decision node {DecisionNodeId} for subflow {SubflowName}, to {TargetId} from {InsertionNodeId}.",
                decisionNodeId, subflow.Name, targetId, insertionNodeId);
        }

        sw.Stop();
        await _hubContext.Clients.All.SendAsync("DetermineInsertionPointsStepDone", sw.ElapsedMilliseconds);

        var result = new List<Flow> { modifiedBasicFlow };
        result.AddRange(subflows);
        return (result, branchingPoints);
    }

    private async Task<string> DetermineInsertionPointAsync(
        Flow basicFlow, Flow subflow, FlowNode targetNode, string useCaseDomain)
    {
        var insertionPrompt = $"""
            Given the basic flow nodes: {JsonSerializer.Serialize(basicFlow.Nodes.Select(n => new { n.Id, n.Label, n.Type }))},
            and a subflow '{subflow.Name}' (Type: {subflow.FlowType}) with entry node '{targetNode.Label}'
            in the {useCaseDomain} domain, suggest the most logical node ID in the basic flow to insert a decision node
            that branches to this subflow. Provide the node ID from the basic flow that the decision node should precede.
            """
            +
            """
            Return JSON: { "InsertionNodeId": "" }
            """;
        var insertionResponse = await _llmService.GenerateContentAsync(insertionPrompt);
        var insertionJson = FlowchartHelpers.ValidateJson(insertionResponse.Content);

        if (insertionJson == null)
        {
            _logger.LogWarning("Invalid JSON response from LLM for insertion point. Defaulting to start node. Response: {Response}",
                insertionResponse.Content);
            return basicFlow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start)?.Id
                ?? throw new InvalidOperationException("Basic flow has no start node.");
        }

        var insertionNodeId = insertionJson["InsertionNodeId"]?.GetValue<string>();
        var yesBranchToSubflow = insertionJson["YesBranchToSubflow"]?.GetValue<bool>() ?? true;

        if (string.IsNullOrEmpty(insertionNodeId) || !basicFlow.Nodes.Any(n => n.Id == insertionNodeId))
        {
            _logger.LogWarning("Invalid insertion node ID {InsertionNodeId} for subflow {SubflowName}. Defaulting to start node.",
                insertionNodeId, subflow.Name);
            insertionNodeId = basicFlow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start)?.Id
                ?? throw new InvalidOperationException("Basic flow has no start node.");
        }

        return insertionNodeId;
    }

    private async Task<string> GenerateDecisionLabelAsync(Flow subflow, FlowNode targetNode, string useCaseDomain)
    {
        string branchingCondition = subflow.FlowType == FlowType.Exception
            ? "'No' branch leads to the subflow and 'Yes' to the basic flow"
            : "'Yes' branch leads to the subflow and 'No' to the basic flow";
        var decisionPrompt = $"""
            For the subflow '{subflow.Name}' (Type: {subflow.FlowType}) with entry node '{targetNode.Label}'
            in the {useCaseDomain} domain, generate a clear and concise decision question to branch to this subflow.
            The question should be phrased such that the {branchingCondition}.
            Ensure the question is relevant to the {useCaseDomain} domain and avoids redundancy with other decision nodes.
            """
            +
            """
            Return JSON: { "DecisionLabel": "" }
            """;
        var decisionResponse = await _llmService.GenerateContentAsync(decisionPrompt);
        var decisionJson = FlowchartHelpers.ValidateJson(decisionResponse.Content);
        var decisionLabel = decisionJson?["DecisionLabel"]?.GetValue<string>()
            ?? $"Branch to {subflow.Name}?";

        return decisionLabel;
    }

}