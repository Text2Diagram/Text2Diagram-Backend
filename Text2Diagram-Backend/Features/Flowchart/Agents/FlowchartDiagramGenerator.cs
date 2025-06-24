
using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<FlowchartDiagramGenerator> _logger;
    private readonly ILLMService _llmService;
    private readonly UseCaseSpecAnalyzerForFlowchart _analyzer;
    private readonly DecisionNodeInserter _decisionNodeInserter;
    private readonly RejoinPointIdentifier _rejoinPointIdentifier;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        ILLMService llmService,
        UseCaseSpecAnalyzerForFlowchart analyzer,
        DecisionNodeInserter decisionNodeInserter,
        RejoinPointIdentifier rejoinPointIdentifier)
    {
        _logger = logger;
        _llmService = llmService;
        _analyzer = analyzer;
        _decisionNodeInserter = decisionNodeInserter;
        _rejoinPointIdentifier = rejoinPointIdentifier;
    }

    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            var useCaseDomain = await _analyzer.GetDomainAsync(input);
            _logger.LogInformation("Use case domain: {0}", useCaseDomain);
            var flows = await _analyzer.AnalyzeAsync(input);
            var (modifiedFlows, branchingPoints) = await _decisionNodeInserter.InsertDecisionNodesAsync(flows, useCaseDomain);
            //flows = await _rejoinPointIdentifier.AddRejoinPointsAsync(modifiedFlows);

            FlowJsonSerializer.SerializeFlowsToJson(flows, _logger);

            string mermaidCode = GenerateMermaidCode(modifiedFlows, branchingPoints);
            return mermaidCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

    private string GenerateMermaidCode(List<Flow> flows, List<(string SubFlowName, string BranchNodeId)> branchingPoints)
    {
        var mermaid = new StringBuilder();
        mermaid.AppendLine("graph TD");

        var allNodes = new Dictionary<string, FlowNode>();
        var allEdges = new List<FlowEdge>();
        var basicFlow = flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow is required.");
        var subflows = flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();

        // Add basic flow nodes and edges
        foreach (var node in basicFlow.Nodes)
        {
            allNodes[node.Id] = node;
        }
        allEdges.AddRange(basicFlow.Edges);

        // Process subflows
        foreach (var subflow in subflows)
        {
            var branchNodeId = branchingPoints.FirstOrDefault(b => b.SubFlowName == subflow.Name).BranchNodeId;
            if (string.IsNullOrEmpty(branchNodeId))
            {
                _logger.LogWarning("Subflow {SubflowName} has no branching point.", subflow.Name);
                mermaid.AppendLine($"    %% Warning: Subflow '{subflow.Name}' is unconnected.");
                continue;
            }

            // Skip start node and add other nodes with unique IDs
            foreach (var node in subflow.Nodes.Where(n => n.Type != NodeType.Start))
            {
                var nodeId = $"{subflow.Name}_{node.Id}";
                if (!allNodes.ContainsKey(nodeId))
                {
                    allNodes[nodeId] = new FlowNode(nodeId, node.Label, node.Type);
                    _logger.LogDebug("Assigned node ID {NodeId} for subflow {SubflowName}, original node {OriginalNodeId}",
                        nodeId, subflow.Name, node.Id);
                }
            }

            // Update subflow edges, skipping those involving start node
            foreach (var edge in subflow.Edges)
            {
                var sourceId = edge.SourceId == "exception_flow_start_1" || edge.SourceId == "alternative_flow_start_1"
                    ? GetFirstNonStartNodeId(subflow, edge)
                    : $"{subflow.Name}_{edge.SourceId}";
                var targetId = edge.TargetId == "exception_flow_start_1" || edge.TargetId == "alternative_flow_start_1"
                    ? GetFirstNonStartNodeId(subflow, edge)
                    : $"{subflow.Name}_{edge.TargetId}";
                if (sourceId == null || targetId == null || sourceId == targetId) continue; // Skip invalid or self-referential edges

                var newEdge = new FlowEdge(sourceId, targetId, edge.Type, edge.Label);
                if (!IsDuplicateEdge(newEdge, allEdges))
                {
                    allEdges.Add(newEdge);
                }
            }
        }

        // Generate node definitions, grouped by flow
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

        // Generate all edges without restrictive filtering
        mermaid.AppendLine("    %% Flow Edges");
        foreach (var edge in allEdges.OrderBy(e => e.SourceId))
        {
            string connector = GetEdgeConnector(edge.Type);
            string edgeLine = string.IsNullOrEmpty(edge.Label)
                ? $"    {edge.SourceId} {connector} {edge.TargetId}"
                : $"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
            if (edge.Label == "Rejoin")
            {
                edgeLine = $"    %% Rejoin to basic flow\n    {edgeLine}";
            }
            mermaid.AppendLine(edgeLine);
        }

        return mermaid.ToString();
    }

    private string? GetFirstNonStartNodeId(Flow subflow, FlowEdge edge)
    {
        var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
        if (startNode == null) return null;

        var nextNode = subflow.Edges
            .Where(e => e.SourceId == startNode.Id)
            .Select(e => subflow.Nodes.FirstOrDefault(n => n.Id == e.TargetId))
            .FirstOrDefault();
        if (nextNode == null) return null;

        return $"{subflow.Name}_{nextNode.Id}";
    }

    private bool IsDuplicateEdge(FlowEdge edge, List<FlowEdge> existingEdges)
    {
        return existingEdges.Any(e =>
            e.SourceId == edge.SourceId &&
            e.TargetId == edge.TargetId &&
            e.Label == edge.Label &&
            e.Type == edge.Type);
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
        string content = label.Replace("\"", "\\\""); // Escape quotes
        return type switch
        {
            NodeType.Start or NodeType.End => $"{id}([{content}])",        // Rounded
            NodeType.Process => $"{id}[{content}]",                          // Rectangle
            NodeType.Subroutine => $"{id}[[{content}]]",                     // Double rectangle
            NodeType.Decision => $"{id}{{{content}}}",                       // Diamond
            NodeType.InputOutput or NodeType.Document => $"{id}[/\"{content}\"/]", // Parallelogram
            NodeType.DataStore => $"{id}[(\"{content}\")]",                  // Cylinder
            NodeType.Comment => $"{id}:::note[{content}]",                   // Note (optional style class)
            _ => $"{id}[{content}]"                                          // Default to rectangle
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
