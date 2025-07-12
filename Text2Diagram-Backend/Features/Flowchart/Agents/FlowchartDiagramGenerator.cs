using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Features.Flowchart.Components;
using Text2Diagram_Backend.Middlewares;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<FlowchartDiagramGenerator> _logger;
    private readonly ILLMService1 _llmService;
    private readonly ILLMService3 _llmService3;
    private readonly UseCaseSpecAnalyzerForFlowchart _analyzer;
    private readonly DecisionNodeInserter _decisionNodeInserter;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly FlowchartDiagramEvaluator _flowchartDiagramEvaluator;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        ILLMService1 llmService,
        ILLMService3 llmService3,
        UseCaseSpecAnalyzerForFlowchart analyzer,
        DecisionNodeInserter decisionNodeInserter,
        IHubContext<ThoughtProcessHub> hubContext,
        FlowchartDiagramEvaluator flowchartDiagramEvaluator)
    {
        _logger = logger;
        _llmService = llmService;
        _llmService3 = llmService3;
        _analyzer = analyzer;
        _decisionNodeInserter = decisionNodeInserter;
        _hubContext = hubContext;
        _flowchartDiagramEvaluator = flowchartDiagramEvaluator;
    }

    public async Task<DiagramContent> GenerateAsync(string input)
    {
        var flows = await _analyzer.AnalyzeAsync(input);


        if (flows == null || !flows.Any())
        {
            _logger.LogError("No flows extracted from use case specification.");
            throw new InvalidOperationException("No flows extracted from use case specification.");
        }

        //_logger.LogInformation("All flows: {Flows}", JsonSerializer.Serialize(flows));

        var (modifiedFlows, branchingPoints) = await _decisionNodeInserter.InsertDecisionNodesAsync(flows);

        //_logger.LogInformation("[After inserting decision nodes] Flows: {Flows}", JsonSerializer.Serialize(modifiedFlows));
        //_logger.LogInformation("[Decision nodes] Flows: {Flows}", JsonSerializer.Serialize(branchingPoints));

        var flowchart = new FlowchartDiagram(modifiedFlows, branchingPoints);

        _logger.LogInformation("Flowchart: {Flowchart}", JsonSerializer.Serialize(flowchart));
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Evaluating the diagram...");
        var validatedFlowchart = await ValidateDiagram(flowchart);
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generating flowchart diagram...");
        string mermaidCode = GenerateMermaidCode(flowchart);

        string jsonString = JsonSerializer.Serialize(flowchart, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        //_logger.LogInformation("{JsonString}", jsonString);

        _logger.LogInformation("Generated Mermaid code: {MermaidCode}", mermaidCode);

        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Flowchart diagram generated!");
        return new DiagramContent
        {
            mermaidCode = mermaidCode,
            diagramJson = jsonString
        };
    }

    public async Task<DiagramContent> ReGenerateAsync(string feedback, string diagramJson)
    {
        _logger.LogInformation("Regenerating flowchart with feedback: {Feedback}", feedback);

        var response = await ApplyCommandsAsync(feedback, diagramJson);
        var jsonNode = FlowchartHelpers.ValidateJson(response);
        if (jsonNode == null)
        {
            _logger.LogError("Invalid JSON response from LLM for feedback: {Feedback}", feedback);
            throw new InvalidOperationException("Invalid JSON response from LLM");
        }

        var flows = jsonNode["Flows"]?.AsArray()
            ?.Select(node => node.Deserialize<Flow>())
            ?.Where(flow => flow != null)
            ?.Cast<Flow>()
            ?.ToList() ?? new List<Flow>();

        var branchingPoints = jsonNode["BranchingPoints"]?.AsArray()
            ?.Select(node => node.Deserialize<BranchingPoint>())
            ?.Where(bp => bp != null)
            ?.Cast<BranchingPoint>()
            ?.ToList() ?? new List<BranchingPoint>();

        if (!flows.Any())
        {
            _logger.LogWarning("No valid flows parsed from JSON");
            throw new InvalidOperationException("No valid flows parsed from JSON");
        }

        var flowchart = new FlowchartDiagram(flows, branchingPoints);
        //await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Validating flowchart diagram...");
        var validatedFlowchart = await ValidateDiagram(flowchart);

        //await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generating flowchart diagram...");
        var mermaidCode = GenerateMermaidCode(validatedFlowchart);

        string jsonString = JsonSerializer.Serialize(validatedFlowchart, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        return new DiagramContent
        {
            mermaidCode = mermaidCode,
            diagramJson = jsonString
        };
    }

    private async Task<string> ApplyCommandsAsync(string feedback, string diagramDataJson)
    {
        _logger.LogInformation("Applying feedback to flowchart JSON using LLM service");

        // Parse the input JSON to ensure it's valid
        JsonNode? jsonNode;
        try
        {
            jsonNode = JsonNode.Parse(diagramDataJson);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to parse diagramDataJson: {Error}", ex.Message);
            throw new InvalidOperationException("Invalid diagram JSON format", ex);
        }

        if (jsonNode == null)
        {
            _logger.LogError("Parsed JSON is null");
            throw new InvalidOperationException("Parsed JSON is null");
        }

        var prompt = $"""
                You are a flowchart diagram modification assistant. 
                Modify the provided flowchart JSON based on the given feedback. 
                Ensure the output is a valid JSON string wrapped in ```json\n...\n``` code fences, 
                maintaining the structure with 'Flows' and 'BranchingPoints'. 
                The diagram is a flowchart, and the JSON includes flows with nodes (Id, Type, Label) 
                and edges (SourceId, TargetId, Type, Label), and branching points (SubFlowName, BranchNodeId). 
                Node Rules: {Prompts.NodeRules}
                Edge Rules: {Prompts.EdgeRules}
                Current JSON: {diagramDataJson}
                Feedback: {feedback}
                Instruction: Interpret the feedback and modify the flowchart JSON accordingly. 
                For example, if the feedback requests adding a node, add it to the appropriate flow's 'Nodes' array and update the 'Edges' array to maintain connectivity. 
                Ensure unique node IDs, valid node/edge types, and a consistent flowchart structure.
                """;
        try
        {
            var llmResponse = await _llmService3.GenerateContentAsync(prompt);
            if (string.IsNullOrEmpty(llmResponse?.Content))
            {
                _logger.LogWarning("LLM returned empty response for feedback: {Feedback}", feedback);
                throw new InvalidOperationException("LLM returned empty response");
            }

            // Validate and extract the JSON from the LLM response
            var updatedJsonNode = FlowchartHelpers.ValidateJson(llmResponse.Content);
            if (updatedJsonNode == null)
            {
                _logger.LogError("LLM response does not contain valid JSON for feedback: {Feedback}", feedback);
                throw new InvalidOperationException("LLM response does not contain valid JSON");
            }

            _logger.LogInformation("Applied feedback to flowchart JSON");
            return llmResponse.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error applying feedback: {Error}", ex.Message);
            throw;
        }
    }

    public string GenerateMermaidCode(FlowchartDiagram flowchart)
    {
        var mermaid = new StringBuilder();
        mermaid.AppendLine("graph TD");

        var allNodes = new Dictionary<string, FlowNode>();
        var allEdges = new List<FlowEdge>();
        var basicFlow = flowchart.Flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow is required.");
        var subflows = flowchart.Flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();

        // Add basic flow nodes and edges
        foreach (var node in basicFlow.Nodes)
        {
            allNodes[node.Id] = node;
        }
        foreach (var edge in basicFlow.Edges)
        {
            var targetNode = allNodes.ContainsKey(edge.TargetId) ? allNodes[edge.TargetId] : null;
            if (targetNode == null)
            {
                _logger.LogWarning("Skipping edge with invalid target node: {SourceId} to {TargetId}", edge.SourceId, edge.TargetId);
                continue;
            }
            if (!allEdges.Any(e => e.SourceId == edge.SourceId && e.TargetId == edge.TargetId && e.Label == edge.Label))
            {
                allEdges.Add(edge);
            }
            else
            {
                _logger.LogWarning("Skipping redundant edge in basic flow: {SourceId} to {TargetId} with label {Label}", edge.SourceId, edge.TargetId, edge.Label);
            }
        }

        // Process subflows
        foreach (var subflow in subflows)
        {
            var branchingPoint = flowchart.BranchingPoints.FirstOrDefault(b => b.SubFlowName == subflow.Name);
            if (branchingPoint == null)
            {
                _logger.LogWarning("Subflow {SubFlowName} has no branching point.", subflow.Name);
                mermaid.AppendLine($"    %% Warning: Subflow '{subflow.Name}' is unconnected.");
                continue;
            }
            var branchNodeId = branchingPoint.BranchNodeId;
            var rejoinNodeId = branchingPoint.RejoinNodeId;
            if (string.IsNullOrEmpty(branchNodeId))
            {
                _logger.LogWarning("Subflow {SubFlowName} has no branching point.", subflow.Name);
                mermaid.AppendLine($"    %% Warning: Subflow '{subflow.Name}' is unconnected.");
                continue;
            }

            var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
            if (startNode == null)
            {
                _logger.LogWarning("Subflow {SubFlowName} has no start node. Skipping.", subflow.Name);
                continue;
            }

            var entryNode = FindFirstNonDecisionNode(subflow, startNode);
            if (entryNode == null)
            {
                _logger.LogWarning("Subflow {SubFlowName} has no valid entry node. Skipping.", subflow.Name);
                continue;
            }

            var reachableNodes = GetReachableNodes(subflow, entryNode);
            foreach (var node in reachableNodes.Where(n => n.Type != NodeType.Start &&
                !(subflow.FlowType == FlowType.Exception && n.Type == NodeType.Decision) &&
                !(subflow.FlowType == FlowType.Alternative && n.Type == NodeType.Decision)))
            {
                var nodeId = $"{subflow.Name}_{node.Id}";
                if (!allNodes.ContainsKey(nodeId))
                {
                    allNodes[nodeId] = new FlowNode(nodeId, node.Label, node.Type);
                    _logger.LogInformation("Assigned node ID {NodeId} for subflow {SubFlowName}, original node {OriginalNodeId}",
                        nodeId, subflow.Name, node.Id);
                }
            }

            foreach (var edge in subflow.Edges)
            {
                var sourceNode = subflow.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
                var targetNode = subflow.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);
                if (sourceNode == null || targetNode == null)
                {
                    _logger.LogWarning("Skipping invalid edge in subflow {SubFlowName}: {SourceId} to {TargetId}",
                        subflow.Name, edge.SourceId, edge.TargetId);
                    continue;
                }

                if (sourceNode.Type == NodeType.Start || targetNode.Type == NodeType.Start ||
                    (subflow.FlowType == FlowType.Exception && (sourceNode.Type == NodeType.Decision || targetNode.Type == NodeType.Decision)) ||
                    (subflow.FlowType == FlowType.Alternative && (sourceNode.Type == NodeType.Decision || targetNode.Type == NodeType.Decision)))
                {
                    _logger.LogWarning("Skipping edge involving start or decision node in subflow {SubFlowName}: {SourceId} to {TargetId}",
                        subflow.Name, edge.SourceId, edge.TargetId);
                    continue;
                }

                var sourceId = $"{subflow.Name}_{edge.SourceId}";
                var targetId = $"{subflow.Name}_{edge.TargetId}";
                if (sourceId == targetId)
                {
                    _logger.LogWarning("Skipping self-referential edge in subflow {SubFlowName}: {SourceId} to {TargetId}",
                        subflow.Name, sourceId, targetId);
                    continue;
                }

                var edgeType = edge.Type == EdgeType.Arrow && subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : edge.Type;
                var newEdge = new FlowEdge(sourceId, targetId, edgeType, edge.Label);
                if (!allEdges.Any(e => e.SourceId == newEdge.SourceId && e.TargetId == newEdge.TargetId && e.Label == newEdge.Label))
                {
                    allEdges.Add(newEdge);
                }
            }

            var entryNodeId = $"{subflow.Name}_{entryNode.Id}";
            var branchEdgeLabel = subflow.FlowType == FlowType.Exception ? "No" : "Yes";
            var branchEdgeType = subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : EdgeType.Arrow;
            var branchEdge = new FlowEdge(branchNodeId, entryNodeId, branchEdgeType, branchEdgeLabel);
            if (!allEdges.Any(e => e.SourceId == branchEdge.SourceId && e.TargetId == branchEdge.TargetId && e.Label == branchEdge.Label))
            {
                allEdges.Add(branchEdge);
            }

            // Add rejoin edge dynamically
            var endNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.End);
            if (endNode != null && !string.IsNullOrEmpty(rejoinNodeId) && allNodes.ContainsKey(rejoinNodeId))
            {
                var endNodeId = $"{subflow.Name}_{endNode.Id}";
                var rejoinEdge = new FlowEdge(endNodeId, rejoinNodeId, EdgeType.Arrow, "Rejoin");
                if (!allEdges.Any(e => e.SourceId == rejoinEdge.SourceId && e.TargetId == rejoinEdge.TargetId && e.Label == rejoinEdge.Label))
                {
                    allEdges.Add(rejoinEdge);
                }
            }
            else
            {
                _logger.LogWarning("No valid rejoin point for subflow {SubFlowName}. End node: {EndNodeId}, Rejoin node: {RejoinNodeId}",
                    subflow.Name, endNode?.Id, rejoinNodeId);
            }
        }

        // Generate node definitions
        mermaid.AppendLine("    %% Basic Flow Nodes");
        foreach (var node in allNodes.Values.Where(n => !subflows.Any(s => n.Id.StartsWith($"{s.Name}_"))))
        {
            string nodeDef = GetNodeWrappedLabel(node.Id, node.Type, node.Label);
            mermaid.AppendLine($"    {nodeDef}");
        }

        foreach (var subflow in subflows)
        {
            mermaid.AppendLine($"    %% {FormatSubFlowName(subflow.Name)} Nodes");
            foreach (var node in allNodes.Values.Where(n => n.Id.StartsWith($"{subflow.Name}_")))
            {
                string nodeDef = GetNodeWrappedLabel(node.Id, node.Type, node.Label);
                mermaid.AppendLine($"    {nodeDef}");
            }
        }

        mermaid.AppendLine();

        // Generate all edges
        mermaid.AppendLine("    %% Flow Edges");
        foreach (var edge in allEdges.OrderBy(e => e.SourceId))
        {
            string connector = GetEdgeConnector(edge.Type);
            bool isRejoinEdge = edge.Label == "Rejoin";
            string edgeLine = isRejoinEdge
                ? $"    %% Rejoin to basic flow\n    {edge.SourceId} {connector} {edge.TargetId}"
                : string.IsNullOrEmpty(edge.Label)
                    ? $"    {edge.SourceId} {connector} {edge.TargetId}"
                    : $"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
            mermaid.AppendLine(edgeLine);
        }

        return mermaid.ToString();
    }

    public async Task<FlowchartDiagram> ValidateDiagram(FlowchartDiagram flowchart)
    {
        var prompt = $"""
        You are tasked with validating and correcting a flowchart to ensure proper connectivity and logical structure. Given the flowchart below, perform the following:

        1. Ensure subflow connectivity:
           - For each subflow, ensure all nodes are connected in a logical sequence from start to end.
           - Add missing edges, e.g., in the applyPromoCode subflow, ensure the validation node connects to both a success path (e.g., apply discount) and a failure path (e.g., display invalid code message).
           - Example: Add edges like applyPromoCode_alternative_flow_subroutine_1 -->|Valid| applyPromoCode_alternative_flow_process_1 and applyPromoCode_alternative_flow_subroutine_1 -->|Invalid| applyPromoCode_alternative_flow_input_output_1 (add the input_output_1 node if missing).
        2. Correct rejoin points:
           - Validate each subflow's rejoin point (RejoinNodeId) to ensure it connects to a logical point in the basic flow.
           - For alternative flows (e.g., applyPromoCode), rejoin at the next logical step (e.g., order processing, not the end).
           - For exception flows (e.g., paymentFailure), rejoin at a retry point (e.g., payment decision) or continuation step.
           - Example: Fix applyPromoCode_alternative_flow_end_1 to rejoin at basic_flow_subroutine_1, not basic_flow_input_3.
        3. Remove redundant decision nodes:
           - Remove decision nodes in subflows (e.g., itemOutOfStock_exception_flow_decision_1) and redirect edges to their successors.
        4. Remove duplicate edges:
           - Remove edges with the same SourceId, TargetId, and Label, preferring Arrow for basic/alternative flows and OpenArrow for exception flows.
        5. Add missing nodes:
           - Add missing nodes like applyPromoCode_alternative_flow_input_output_1 (Label: "Display invalid code message", Type: InputOutput) if needed.
        6. Ensure edge types:
           - Use OpenArrow for exception flow branches (e.g., decision_itemOutOfStock --o|No| itemOutOfStock_exception_flow_input_output_1).
           - Use Arrow for basic and alternative flows and rejoin edges.

        Node Rules: {Prompts.NodeRules}
        Edge Rules: {Prompts.EdgeRules}

        Flowchart: {JsonSerializer.Serialize(flowchart)}
        """
        +
        """
        Return JSON:
        ```json
        {"Flows": [
                {
                    "Name": string,
                    "FlowType": string,
                    "Nodes": [{"Id": string, "Label": string, "Type": string}, ...],
                    "Edges": [{"SourceId": string, "TargetId": string, "Label": string, "Type": string}, ...]
                }, ...
            ],
            "BranchingPoints": [
                {"SubFlowName": string, "BranchNodeId": string, "RejoinNodeId": string}, ...
            ]
        }
        ```
        Include all flows, nodes, edges, and branching points, even if unchanged. Ensure valid JSON wrapped in json\n...\n code fences.
        Do not include additional text or explanations.
        """;

        try
        {
            var response = await _llmService.GenerateContentAsync(prompt);
            _logger.LogInformation("LLM validation response: {Response}", response.Content);

            var jsonResponse = FlowchartHelpers.ValidateJson(response.Content);
            if (jsonResponse == null)
            {
                _logger.LogWarning("Invalid JSON response from LLM validation. Returning original flowchart.");
                return flowchart;
            }

            var flows = jsonResponse["Flows"]?.AsArray()
                                            ?.Select(node => node.Deserialize<Flow>())
                                            ?.Where(flow => flow != null)
                                            ?.Cast<Flow>()
                                            ?.ToList() ?? new List<Flow>();

            var branchingPoints = jsonResponse["BranchingPoints"]?.AsArray()
            ?.Select(node => node.Deserialize<BranchingPoint>())
            ?.Where(bp => bp != null)
            ?.Cast<BranchingPoint>()
            ?.ToList() ?? new List<BranchingPoint>();


            if (!flows.Any() || flows.All(f => f.FlowType != FlowType.Basic))
            {
                _logger.LogWarning("LLM returned invalid flows or missing basic flow. Returning original flowchart.");
                return flowchart;
            }

            var validatedFlowchart = new FlowchartDiagram(flows, branchingPoints);
            _logger.LogInformation("Flowchart validated successfully.");
            return validatedFlowchart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM validation failed. Returning original flowchart.");
            return flowchart;
        }
    }

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

    private List<FlowNode> GetReachableNodes(Flow subflow, FlowNode entryNode)
    {
        var reachableNodes = new List<FlowNode>();
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(entryNode.Id);

        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            if (visited.Contains(currentNodeId))
                continue;
            visited.Add(currentNodeId);

            var currentNode = subflow.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (currentNode != null)
            {
                if (subflow.FlowType == FlowType.Exception && currentNode.Type == NodeType.Decision)
                {
                    _logger.LogWarning("Skipping decision node {NodeId} in exception subflow {SubflowName}", currentNodeId, subflow.Name);
                    continue;
                }
                reachableNodes.Add(currentNode);
                var nextNodes = subflow.Edges
                    .Where(e => e.SourceId == currentNodeId)
                    .Select(e => subflow.Nodes.FirstOrDefault(n => n.Id == e.TargetId))
                    .Where(n => n != null);
                foreach (var nextNode in nextNodes)
                {
                    queue.Enqueue(nextNode!.Id);
                }
            }
        }

        return reachableNodes;
    }

    private string FormatSubFlowName(string name)
    {
        return string.Join(" ", name.Split('_')
            .Select(word => word.Length > 0
                ? char.ToUpper(word[0]) + word.Substring(1)
                : word));
    }

    private string GetNodeWrappedLabel(string id, NodeType type, string label)
    {
        string content = label.Replace("\"", "\\\"");
        return type switch
        {
            NodeType.Start or NodeType.End => $"{id}([{content}])",
            NodeType.Process => $"{id}[{content}]",
            NodeType.Subroutine => $"{id}[[{content}]]",
            NodeType.Decision => $"{id}{{{content}}}",
            NodeType.InputOutput or NodeType.Document => $"{id}[/\"{content}\"/]",
            NodeType.DataStore => $"{id}[(\"{content}\")]",
            NodeType.Comment => $"{id}:::note[{content}]",
            _ => $"{id}[{content}]"
        };
    }

    private string GetEdgeConnector(EdgeType edgeType)
    {
        return edgeType switch
        {
            EdgeType.Arrow => "-->",
            EdgeType.OpenArrow => "--o",
            EdgeType.CrossArrow => "--x",
            EdgeType.NoArrow => "---",
            _ => "-->"
        };
    }
}