using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Text2Diagram_Backend.Authentication;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<FlowchartDiagramGenerator> _logger;
    private readonly ILLMService _llmService;
    private readonly UseCaseSpecAnalyzerForFlowchart _analyzer;
    private readonly DecisionNodeInserter _decisionNodeInserter;
    private readonly RejoinPointIdentifier _rejoinPointIdentifier;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        ILLMService llmService,
        UseCaseSpecAnalyzerForFlowchart analyzer,
        DecisionNodeInserter decisionNodeInserter,
        RejoinPointIdentifier rejoinPointIdentifier,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IHubContext<ThoughtProcessHub> hubContext)
    {
        _logger = logger;
        _llmService = llmService;
        _analyzer = analyzer;
        _decisionNodeInserter = decisionNodeInserter;
        _rejoinPointIdentifier = rejoinPointIdentifier;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
    }

    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            await _hubContext.Clients.All.SendAsync("GetDomainStep", "Determining business domain...");
            var useCaseDomain = await _analyzer.GetDomainAsync(input);
            _logger.LogInformation("Use case domain: {0}", useCaseDomain);
            sw.Stop();
            await _hubContext.Clients.All.SendAsync("GetDomainStepDone", sw.ElapsedMilliseconds);


            var flows = await _analyzer.AnalyzeAsync(input);

            var (modifiedFlows, branchingPoints) = await _decisionNodeInserter.InsertDecisionNodesAsync(flows, useCaseDomain);
            modifiedFlows = await _rejoinPointIdentifier.AddRejoinPointsAsync(modifiedFlows);

            var flowchart = new FlowchartDiagram(modifiedFlows, branchingPoints);

            string jsonString = JsonSerializer.Serialize(flowchart, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            _logger.LogInformation("{JsonString}", jsonString);

            var userId = _httpContextAccessor?.HttpContext?.User.GetUserId();
            var tempDiagram = new TempDiagram()
            {
                DiagramData = jsonString,
                DiagramType = DiagramType.Flowchart,
                CreatedAt = DateTime.UtcNow,
                UserId = userId ?? throw new ArgumentNullException(nameof(userId))
            };
            _dbContext.TempDiagrams.Add(tempDiagram);

            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("GenerateDiagramStep", "Generating flowchart...");

            string mermaidCode = await GenerateMermaidCodeAsync(flowchart);
            return mermaidCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

    public async Task<string> GenerateMermaidCodeAsync(FlowchartDiagram flowchart)
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
            var isExceptionDecision = subflows.Any(s => s.FlowType == FlowType.Exception && edge.TargetId.StartsWith($"{s.Name}_") && targetNode?.Type == NodeType.Decision);
            if (!isExceptionDecision && !allEdges.Any(e => e.SourceId == edge.SourceId && e.TargetId == edge.TargetId && e.Label == edge.Label))
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
                continue;
            }
            var branchNodeId = branchingPoint.BranchNodeId;
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
            foreach (var node in reachableNodes.Where(n => n.Type != NodeType.Start && !(subflow.FlowType == FlowType.Exception && n.Type == NodeType.Decision)))
            {
                var nodeId = $"{subflow.Name}_{node.Id}";
                if (!allNodes.ContainsKey(nodeId))
                {
                    allNodes[nodeId] = new FlowNode(nodeId, node.Label, node.Type);
                    _logger.LogWarning("Assigned node ID {NodeId} for subflow {SubFlowName}, original node {OriginalNodeId}",
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
                    (subflow.FlowType == FlowType.Exception && (sourceNode.Type == NodeType.Decision || targetNode.Type == NodeType.Decision)))
                {
                    _logger.LogWarning("Skipping edge involving start or decision node in exception subflow {SubFlowName}: {SourceId} to {TargetId}",
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

                var newEdge = new FlowEdge(sourceId, targetId, edge.Type, edge.Label);
                if (!allEdges.Any(e => e.SourceId == newEdge.SourceId && e.TargetId == newEdge.TargetId && e.Label == newEdge.Label))
                {
                    allEdges.Add(newEdge);
                }
            }

            var entryNodeId = $"{subflow.Name}_{entryNode.Id}";
            var branchEdgeLabel = subflow.FlowType == FlowType.Exception ? "No" : "Yes";
            var branchEdge = new FlowEdge(branchNodeId, entryNodeId, EdgeType.Arrow, branchEdgeLabel);
            if (!allEdges.Any(e => e.SourceId == branchEdge.SourceId && e.TargetId == branchEdge.TargetId && e.Label == branchEdge.Label))
            {
                allEdges.Add(branchEdge);
            }
        }

        // Cleanup: Use LLM to fix duplicates, incorrect sequence, and add rejoin edges
        (allNodes, allEdges) = await CleanupFlowchartAsync(allNodes, allEdges, flowchart.Flows, subflows);

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
            bool isRejoinEdge = edge.SourceId.EndsWith("_end_1") && edge.TargetId.StartsWith("basic_flow_");
            string edgeLine = isRejoinEdge
                ? $"    %% Rejoin to basic flow\n    {edge.SourceId} {connector} {edge.TargetId}"
                : string.IsNullOrEmpty(edge.Label) || edge.Label == "Rejoin"
                    ? $"    {edge.SourceId} {connector} {edge.TargetId}"
                    : $"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
            mermaid.AppendLine(edgeLine);
        }

        return mermaid.ToString();
    }

    private async Task<(Dictionary<string, FlowNode>, List<FlowEdge>)> CleanupFlowchartAsync(
        Dictionary<string, FlowNode> allNodes,
        List<FlowEdge> allEdges,
        List<Flow> flows,
        List<Flow> subflows)
    {
        var basicFlow = flows.First(f => f.FlowType == FlowType.Basic);
        var nodesJson = JsonSerializer.Serialize(allNodes.Values.Select(n => new { n.Id, n.Label, Type = n.Type.ToString() }));
        var edgesJson = JsonSerializer.Serialize(allEdges.Select(e => new { e.SourceId, e.TargetId, e.Label, Type = e.Type.ToString() }));

        var prompt = $"""
            You are tasked with cleaning up a flowchart for a ride-hailing app. Given the nodes and edges below, perform the following:
            1. **Remove duplicate edges**: Remove edges with the same SourceId, TargetId, and Label, keeping only one (prefer Arrow over OpenArrow). For example, remove duplicates like:
               - decision_locationServicesDisabled -->|No| locationServicesDisabled_exception_flow_inputoutput_1 and decision_locationServicesDisabled --o|No| locationServicesDisabled_exception_flow_inputoutput_1
            2. **Correct basic flow sequence**: Ensure the basic flow follows these exact edges:
               - basic_flow_start_1 --> decision_locationServicesDisabled
               - decision_locationServicesDisabled -->|Yes| basic_flow_input_1
               - basic_flow_input_1 --> basic_flow_input_2
               - basic_flow_input_2 --> basic_flow_input_3
               - basic_flow_input_3 --> decision_scheduleRide
               - decision_scheduleRide -->|No| decision_paymentIssue
               - decision_paymentIssue -->|Yes| decision_chooseSpecificDriver
               - decision_chooseSpecificDriver -->|No| basic_flow_subroutine_1
               - basic_flow_subroutine_1 --> decision_noDriversAvailable
               - decision_noDriversAvailable -->|Yes| basic_flow_process_1
               - basic_flow_process_1 --> basic_flow_end_1
               Remove incorrect edges, e.g.:
               - basic_flow_input_3 --> decision_chooseSpecificDriver
               - decision_chooseSpecificDriver -->|No| decision_scheduleRide
               - decision_scheduleRide -->|No| decision_paymentIssue
            3. **Validate rejoin edges**: Ensure these rejoin edges are present:
               - locationServicesDisabled_exception_flow_end_1 -->|Rejoin| basic_flow_input_1
               - chooseSpecificDriver_alternative_flow_end_1 -->|Rejoin| basic_flow_process_1
               Remove any rejoin edges for:
               - scheduleRide_alternative_flow_end_1
               - noDriversAvailable_exception_flow_end_1
               - paymentIssue_exception_flow_end_1
            4. **Remove redundant decision nodes and edges in exception flows**: Remove nodes and their edges, e.g.:
               - locationServicesDisabled_exception_flow_decision_1
               - noDriversAvailable_exception_flow_decision_1
               - paymentIssue_exception_flow_decision_1
               Redirect edges targeting these nodes to their successors, e.g.:
               - decision_locationServicesDisabled -->|No| locationServicesDisabled_exception_flow_inputoutput_1
               - decision_noDriversAvailable -->|No| noDriversAvailable_exception_flow_inputoutput_1
               - decision_paymentIssue -->|No| paymentIssue_exception_flow_inputoutput_1
            5. **Ensure node connectivity**: Add missing edges in subflows, e.g.:
               - chooseSpecificDriver_alternative_flow_input_1 --> chooseSpecificDriver_alternative_flow_subroutine_1
               - chooseSpecificDriver_alternative_flow_subroutine_1 --> chooseSpecificDriver_alternative_flow_end_1
               - scheduleRide_alternative_flow_subroutine_1 --> scheduleRide_alternative_flow_end_1
               - locationServicesDisabled_exception_flow_inputoutput_1 --> locationServicesDisabled_exception_flow_end_1
               - noDriversAvailable_exception_flow_inputoutput_1 --> noDriversAvailable_exception_flow_inputoutput_2
               - noDriversAvailable_exception_flow_inputoutput_2 --> noDriversAvailable_exception_flow_end_1
               - paymentIssue_exception_flow_inputoutput_1 --> paymentIssue_exception_flow_end_1
            6. **Ensure chooseSpecificDriver input node**: Add node chooseSpecificDriver_alternative_flow_input_1 (Label: "User selects a specific driver from a list", Type: InputOutput) if missing, with edge:
               - decision_chooseSpecificDriver -->|Yes| chooseSpecificDriver_alternative_flow_input_1

            Nodes: {nodesJson}
            Edges: {edgesJson}
            """
            +
            """
            Return JSON:
            {
                "Nodes": [{"Id": string, "Label": string, "Type": string}, ...],
                "Edges": [{"SourceId": string, "TargetId": string, "Label": string, "Type": string}, ...]
            }
            """;

        try
        {
            var response = await _llmService.GenerateContentAsync(prompt);
            _logger.LogWarning("LLM cleanup response: {Response}", response.Content);
            var jsonResponse = FlowchartHelpers.ValidateJson(response.Content);
            if (jsonResponse == null)
            {
                _logger.LogWarning("Invalid JSON response from LLM cleanup. Falling back to original nodes and edges.");
                return (allNodes, allEdges);
            }

            var cleanedNodes = new Dictionary<string, FlowNode>();
            var cleanedEdges = new List<FlowEdge>();

            if (jsonResponse["Nodes"]?.AsArray() is JsonArray nodesArray)
            {
                cleanedNodes = nodesArray
                    .Where(n => n != null)
                    .Select(n =>
                    {
                        try
                        {
                            var id = n!["Id"]?.GetValue<string>() ?? throw new JsonException("Node Id is missing");
                            var label = n["Label"]?.GetValue<string>() ?? throw new JsonException("Node Label is missing");
                            var type = n["Type"]?.GetValue<string>() ?? throw new JsonException("Node Type is missing");
                            return new FlowNode(id, label, Enum.Parse<NodeType>(type));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to parse node: {ErrorMessage}", ex.Message);
                            return null;
                        }
                    })
                    .Where(n => n != null)
                    .ToDictionary(n => n!.Id, n => n!);
            }

            if (jsonResponse["Edges"]?.AsArray() is JsonArray edgesArray)
            {
                cleanedEdges = edgesArray
                    .Where(e => e != null)
                    .Select(e =>
                    {
                        try
                        {
                            var sourceId = e!["SourceId"]?.GetValue<string>() ?? throw new JsonException("Edge SourceId is missing");
                            var targetId = e["TargetId"]?.GetValue<string>() ?? throw new JsonException("Edge TargetId is missing");
                            var type = e["Type"]?.GetValue<string>() ?? throw new JsonException("Edge Type is missing");
                            var label = e["Label"]?.GetValue<string>();
                            return new FlowEdge(sourceId, targetId, Enum.Parse<EdgeType>(type), label);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to parse edge: {ErrorMessage}", ex.Message);
                            return null;
                        }
                    })
                    .Where(e => e != null)
                    .ToList()!;
            }

            if (!cleanedNodes.Any() || cleanedEdges == null)
            {
                _logger.LogWarning("LLM returned invalid nodes or edges. Falling back to original.");
                return (allNodes, allEdges);
            }

            foreach (var node in allNodes)
            {
                if (!cleanedNodes.ContainsKey(node.Key))
                {
                    _logger.LogWarning("Preserving original node: {NodeId}", node.Key);
                    cleanedNodes[node.Key] = node.Value;
                }
            }

            return (cleanedNodes, cleanedEdges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM cleanup failed. Falling back to original nodes and edges.");
            return (allNodes, allEdges);
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