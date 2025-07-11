using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Features.Flowchart.Components;
using Text2Diagram_Backend.LLMServices;
using Text2Diagram_Backend.Middlewares;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class DecisionNodeInserter
{
    private readonly ILLMService1 _llmService;
    private readonly ILLMService2 _llmService2;
    private readonly ILLMService3 _llmService3;
    private readonly AiTogetherService _aiTogetherService;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly ILogger<DecisionNodeInserter> _logger;
    private readonly object _basicFlowLock = new object();
    private readonly SemaphoreSlim _llmSemaphore = new SemaphoreSlim(10); // Limit to 10 concurrent LLM calls

    public DecisionNodeInserter(
        ILLMService1 llmService,
        ILLMService2 llmService2,
        ILLMService3 llmService3,
        AiTogetherService aiTogetherService,
        IHubContext<ThoughtProcessHub> hubContext,
        ILogger<DecisionNodeInserter> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _llmService2 = llmService2;
        _llmService3 = llmService3;
        _aiTogetherService = aiTogetherService;
        _hubContext = hubContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(List<Flow> Flows, List<BranchingPoint> BranchingPoints)>
    InsertDecisionNodesAsync(List<Flow> flows)
    {
        var branchingPoints = new ConcurrentBag<BranchingPoint>();
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
        var modifiedSubflows = new ConcurrentBag<Flow>();

        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Processing subflows in parallel...");

        await Parallel.ForEachAsync(subflows, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (subflow, ct) =>
        {
            // Step 1: Find start and target nodes
            var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
            if (startNode == null)
            {
                _logger.LogWarning("Subflow {SubflowName} is missing a start node. Skipping.", subflow.Name);
                return;
            }

            var targetNode = subflow.Edges
                .Where(e => e.SourceId == startNode.Id)
                .Select(e => subflow.Nodes.FirstOrDefault(n => n.Id == e.TargetId))
                .FirstOrDefault();

            if (targetNode == null)
            {
                _logger.LogWarning("Subflow {SubflowName} has no valid entry node. Skipping.", subflow.Name);
                return;
            }

            // Step 2: Bypass decision nodes in subflows
            Flow modifiedSubflow = subflow;
            if (targetNode.Type == NodeType.Decision)
            {
                _logger.LogWarning("Found decision node {NodeId} in subflow {SubflowName}. Bypassing to non-decision node.", targetNode.Id, subflow.Name);
                var nextNode = FindFirstNonDecisionNode(subflow, targetNode);
                if (nextNode == null)
                {
                    _logger.LogWarning("Subflow {SubflowName} has no valid non-decision entry node. Skipping.", subflow.Name);
                    return;
                }
                targetNode = nextNode;

                var newEdges = subflow.Edges
                    .Where(e => e.SourceId != startNode.Id || e.TargetId != targetNode.Id)
                    .ToList();
                newEdges.Add(new FlowEdge(startNode.Id, targetNode.Id, EdgeType.Arrow, null));
                modifiedSubflow = new Flow(subflow.Name, subflow.FlowType, subflow.Nodes, newEdges);
            }

            var decisionNodeId = $"decision_{subflow.Name}";
            var targetId = $"{subflow.Name}_{targetNode.Id}";

            // Step 3: Perform LLM-based operations sequentially for this subflow
            await _llmSemaphore.WaitAsync(ct);
            try
            {
                // Determine insertion point
                var insertionNodeId = await DetermineInsertionPointAsync(modifiedBasicFlow, subflow, targetNode);

                // Determine rejoin point
                var rejoinNodeId = await DetermineRejoinPointAsync(modifiedBasicFlow, insertionNodeId, subflow);

                // Generate decision label
                var decisionLabel = await GenerateDecisionLabelAsync(subflow, targetNode);

                // Step 4: Prepare updates (thread-safe)
                var decisionNode = new FlowNode(decisionNodeId, decisionLabel, NodeType.Decision);
                var basicFlowLabel = subflow.FlowType == FlowType.Exception ? "Yes" : "No";
                var subflowLabel = subflow.FlowType == FlowType.Exception ? "No" : "Yes";
                var subflowEdgeType = subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : EdgeType.Arrow;

                // Step 5: Update shared state with synchronization
                lock (_basicFlowLock)
                {
                    if (!modifiedBasicFlow.Nodes.Any(n => n.Id == decisionNodeId))
                    {
                        modifiedBasicFlow.Nodes.Add(decisionNode);
                    }

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
                            subflowEdgeType,
                            subflowLabel
                        ));
                    }
                    else
                    {
                        _logger.LogWarning("No insertion edge found for node {InsertionNodeId} in subflow {SubflowName}. Skipping.",
                            insertionNodeId, subflow.Name);
                        return;
                    }
                }

                // Step 6: Add to concurrent collections
                modifiedSubflows.Add(modifiedSubflow);
                branchingPoints.Add(new BranchingPoint(subflow.Name, decisionNodeId, rejoinNodeId));

                _logger.LogInformation("Inserted decision node {DecisionNodeId} for subflow {SubflowName}, to {TargetId} from {InsertionNodeId}, rejoin at {RejoinNodeId}.",
                    decisionNodeId, subflow.Name, targetId, insertionNodeId, rejoinNodeId);
            }
            finally
            {
                _llmSemaphore.Release();
            }
        });

        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Subflow processing completed.");

        var result = new List<Flow> { modifiedBasicFlow };
        result.AddRange(modifiedSubflows);
        return (result, branchingPoints.ToList());
    }

    private async Task<string> DetermineInsertionPointAsync(Flow basicFlow, Flow subflow, FlowNode targetNode)
    {
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", $"Determining decision node for subflow {subflow.Name}...");

        var insertionPrompt = $"""
            Given the basic flow nodes: {JsonSerializer.Serialize(basicFlow.Nodes.Select(n => new { n.Id, n.Label, n.Type }))},
            and a subflow '{subflow.Name}' (Type: {subflow.FlowType}) with entry node '{targetNode.Label}',
            suggest the most logical node ID in the basic flow to insert a decision node
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
        if (string.IsNullOrEmpty(insertionNodeId) || !basicFlow.Nodes.Any(n => n.Id == insertionNodeId))
        {
            _logger.LogWarning("Invalid insertion node ID {InsertionNodeId} for subflow {SubflowName}. Defaulting to start node.",
                insertionNodeId, subflow.Name);
            return basicFlow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start)?.Id
                ?? throw new InvalidOperationException("Basic flow has no start node.");
        }

        return insertionNodeId;
    }

    private async Task<string> GenerateDecisionLabelAsync(Flow subflow, FlowNode targetNode)
    {
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", $"Generating label for decision edge from subflow {subflow.Name} to main flow...");

        string branchingCondition = subflow.FlowType == FlowType.Exception
            ? "'No' branch leads to the subflow and 'Yes' to the basic flow"
            : "'Yes' branch leads to the subflow and 'No' to the basic flow";
        var decisionPrompt = $"""
            For the subflow '{subflow.Name}' (Type: {subflow.FlowType}) with entry node '{targetNode.Label}',
            generate a clear and concise decision question to branch to this subflow.
            The question should be phrased such that the {branchingCondition}.
            Avoids redundancy with other decision nodes.
            {Prompts.LanguageRules}
            """
            +
            """
            Return JSON: { "DecisionLabel": "" }
            """;
        var decisionResponse = await _llmService2.GenerateContentAsync(decisionPrompt);
        var decisionJson = FlowchartHelpers.ValidateJson(decisionResponse.Content);
        return decisionJson?["DecisionLabel"]?.GetValue<string>() ?? $"Branch to {subflow.Name}?";
    }

    private async Task<string> DetermineRejoinPointAsync(Flow basicFlow, string insertionNodeId, Flow subflow)
    {
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", $"Determining rejoin node for subflow {subflow.Name}...");

        var basicFlowNodesJson = JsonSerializer.Serialize(basicFlow.Nodes.Select(n => new { n.Id, n.Label, n.Type }));
        var subflowJson = JsonSerializer.Serialize(new
        {
            subflow.Name,
            subflow.FlowType,
            Nodes = subflow.Nodes.Select(n => new { n.Id, n.Label, n.Type }),
            Edges = subflow.Edges.Select(e => new { e.SourceId, e.TargetId, e.Label, e.Type })
        });
        var insertionNode = basicFlow.Nodes.FirstOrDefault(n => n.Id == insertionNodeId);

        string insertionNodeJson = "{" + $"""
            "Id": "{insertionNodeId}", "Label": "{insertionNode?.Label ?? "Unknown"}", "Type": "{insertionNode?.Type.ToString() ?? "Unknown"}"
            """ + "}";

        var prompt = $"""
            You are tasked with determining the rejoin point for a subflow in a flowchart. The subflow should reconnect to the basic flow at a logical point based on its purpose and the flowchart's structure.
            Basic Flow Nodes: {basicFlowNodesJson}
            Subflow: {subflowJson}
            Insertion Node: {insertionNodeJson}
            Instructions:
            1. Analyze the subflow's purpose based on its name, type (Alternative or Exception), and nodes (e.g., modifying a cart, applying a promo code, handling out-of-stock items, or payment failure).
            2. Determine the most logical node in the basic flow for the subflow to rejoin, based on the process flow:
               - For alternative flows (e.g., cart modification, promo code), rejoin at the next logical step in the main process (e.g., after cart modification, rejoin at confirming delivery address).
               - For exception flows (e.g., out-of-stock, payment failure), rejoin at a step that allows retrying or continuing the process (e.g., payment failure rejoins at the payment decision node).
            3. Return the node ID from the basic flow (e.g., "basic_flow_input_3") where the subflow should rejoin.
            4. If no suitable rejoin point is found, return the basic flow's end node ID as a fallback.
            """
            +
            """
            Return JSON: { "RejoinNodeId": string }
            """;

        try
        {
            var response = await _llmService3.GenerateContentAsync(prompt);
            _logger.LogInformation("LLM rejoin point response for subflow {SubflowName}: {Response}", subflow.Name, response.Content);

            var jsonResponse = FlowchartHelpers.ValidateJson(response.Content);
            if (jsonResponse == null || jsonResponse["RejoinNodeId"]?.GetValue<string>() is not string rejoinNodeId)
            {
                _logger.LogWarning("Invalid JSON response from LLM for rejoin point in subflow {SubflowName}. Falling back to deterministic logic. Response: {Response}",
                    subflow.Name, response.Content);
                return FallbackRejoinPoint(basicFlow, insertionNodeId, subflow);
            }
            if (basicFlow.Nodes.Any(n => n.Id == rejoinNodeId))
            {
                _logger.LogInformation("LLM selected rejoin point {RejoinNodeId} for subflow {SubflowName}.", rejoinNodeId, subflow.Name);
                return rejoinNodeId;
            }

            _logger.LogWarning("LLM returned invalid rejoin node {RejoinNodeId} for subflow {SubflowName}. Falling back to deterministic logic.", rejoinNodeId, subflow.Name);
            return FallbackRejoinPoint(basicFlow, insertionNodeId, subflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM rejoin point determination failed for subflow {SubflowName}. Falling back to deterministic logic.", subflow.Name);
            return FallbackRejoinPoint(basicFlow, insertionNodeId, subflow);
        }
    }

    // Unchanged methods: FindFirstNonDecisionNode and FallbackRejoinPoint
    private FlowNode? FindFirstNonDecisionNode(Flow subflow, FlowNode startNode)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startNode.Id);

        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            if (visited.Contains(currentNodeId))
                continue;
            visited.Add(currentNodeId);

            var currentNode = subflow.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (currentNode == null)
                continue;

            if (currentNode.Type != NodeType.Decision && currentNode.Type != NodeType.Start)
                return currentNode;

            if (currentNode.Type == NodeType.Decision && subflow.FlowType == FlowType.Alternative)
            {
                _logger.LogWarning("Found decision node {NodeId} in alternative subflow {SubflowName}. Bypassing to find non-decision node.", currentNode.Id, subflow.Name);
            }

            var nextNodes = subflow.Edges
                .Where(e => e.SourceId == currentNodeId)
                .Select(e => subflow.Nodes.FirstOrDefault(n => n.Id == e.TargetId))
                .Where(n => n != null);
            foreach (var nextNode in nextNodes)
            {
                queue.Enqueue(nextNode!.Id);
            }
        }

        return null;
    }

    private string FallbackRejoinPoint(Flow basicFlow, string insertionNodeId, Flow subflow)
    {
        var insertionEdge = basicFlow.Edges.FirstOrDefault(e => e.SourceId == insertionNodeId);
        if (insertionEdge != null)
        {
            var targetNode = basicFlow.Nodes.FirstOrDefault(n => n.Id == insertionEdge.TargetId);
            if (targetNode?.Type == NodeType.Decision && subflow.FlowType == FlowType.Exception)
            {
                if (subflow.Name.Contains("paymentFailure", StringComparison.OrdinalIgnoreCase))
                {
                    return targetNode.Id;
                }
                var yesEdge = basicFlow.Edges.FirstOrDefault(e => e.SourceId == targetNode.Id && e.Label == "Yes");
                if (yesEdge != null)
                {
                    return yesEdge.TargetId;
                }
            }
            return insertionEdge.TargetId;
        }

        var insertionNodeIndex = basicFlow.Nodes.FindIndex(n => n.Id == insertionNodeId);
        if (insertionNodeIndex >= 0 && insertionNodeIndex + 1 < basicFlow.Nodes.Count)
        {
            var nextNode = basicFlow.Nodes[insertionNodeIndex + 1];
            if (subflow.FlowType == FlowType.Alternative && nextNode.Type == NodeType.Decision)
            {
                var yesEdge = basicFlow.Edges.FirstOrDefault(e => e.SourceId == nextNode.Id && e.Label == "Yes");
                return yesEdge?.TargetId ?? nextNode.Id;
            }
            return nextNode.Id;
        }

        var endNode = basicFlow.Nodes.FirstOrDefault(n => n.Type == NodeType.End);
        if (endNode != null)
        {
            return endNode.Id;
        }

        _logger.LogWarning("No valid fallback rejoin point found for insertion node {InsertionNodeId} in subflow {SubflowName}.", insertionNodeId, subflow.Name);
        throw new InvalidOperationException($"No valid rejoin point found for subflow {subflow.Name}.");
    }
}