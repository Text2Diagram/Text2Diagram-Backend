using System.Text;
using System.Text.Json;
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
            var flows = await _analyzer.AnalyzeAsync(input);
            var (modifiedFlows, branchingPoints) = await _decisionNodeInserter.InsertDecisionNodesAsync(flows);
            flows = await _rejoinPointIdentifier.AddRejoinPointsAsync(modifiedFlows);
            string mermaidCode = GenerateMermaidCode(flows, branchingPoints);
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
        var subflowConnections = new List<(string SubflowName, FlowEdge Edge)>();

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
                mermaid.AppendLine($"    %% Warning: Subflow '{subflow.Name}' is unconnected.");
                continue;
            }

            foreach (var node in subflow.Nodes)
            {
                var nodeId = $"{subflow.Name}_{node.Id}";
                allNodes[nodeId] = new FlowNode(nodeId, node.Label, node.Type);
                _logger.LogDebug("Assigned node ID {0} for subflow {1}, original node {2}", nodeId, subflow.Name, node.Id);
                // Update edges to use new node IDs
                subflow.Edges.Where(e => e.SourceId == node.Id).ToList().ForEach(e => e.SourceId = nodeId);
                subflow.Edges.Where(e => e.TargetId == node.Id).ToList().ForEach(e => e.TargetId = nodeId);
            }

            // Add subflow edges, including rejoin edges
            allEdges.AddRange(subflow.Edges.Where(e => !IsDuplicateEdge(e, allEdges)));

            // Connect subflow to main flow
            var startNode = subflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
            if (startNode != null)
            {
                var connectionEdge = new FlowEdge
                (
                    branchNodeId,
                    $"{subflow.Name}_start_1",
                    subflow.FlowType == FlowType.Exception ? EdgeType.OpenArrow : EdgeType.Arrow,
                    ""
                );

                if (!IsDuplicateEdge(connectionEdge, allEdges))
                {
                    subflowConnections.Add((subflow.Name, connectionEdge));
                }
            }
            else
            {
                _logger.LogWarning("Subflow {0} has no Start node.", subflow.Name);
            }
        }

        // Validate decision nodes
        var decisionNodes = allNodes.Values.Where(n => n.Type == NodeType.Decision).ToList();
        foreach (var subflow in subflows)
        {
            if (!decisionNodes.Any(n => n.Id.StartsWith($"decision_{subflow.Name}")))
            {
                _logger.LogWarning("No decision node found for subflow {0}.", subflow.Name);
                mermaid.AppendLine($"    %% Warning: Missing decision node for subflow '{subflow.Name}'.");
            }
        }

        // Generate node definitions
        foreach (var node in allNodes.Values)
        {
            string nodeDef = GetNodeWrappedLabel(node.Id, node.Type, node.Label);
            mermaid.AppendLine($"    {nodeDef}");
        }

        mermaid.AppendLine();

        // Generate edges (basic flow and rejoin edges)
        foreach (var edge in allEdges)
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

        // Generate subgraphs for subflows (inline small subflows)
        foreach (var subflow in subflows)
        {
            if (subflow.Nodes.Count <= 3) // Inline small subflows
            {
                mermaid.AppendLine($"    %% Inline subflow: {subflow.Name}");
                foreach (var edge in subflow.Edges.Where(e => e.Label != "Rejoin"))
                {
                    string connector = GetEdgeConnector(edge.Type);
                    string edgeLine = string.IsNullOrEmpty(edge.Label)
                        ? $"    {edge.SourceId} {connector} {edge.TargetId}"
                        : $"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
                    mermaid.AppendLine(edgeLine);
                }
            }
            else
            {
                mermaid.AppendLine();
                mermaid.AppendLine($"    subgraph {subflow.Name}[\"{FormatSubFlowName(subflow.Name)}\"]");
                foreach (var edge in subflow.Edges.Where(e => e.Label != "Rejoin"))
                {
                    string connector = GetEdgeConnector(edge.Type);
                    string edgeLine = string.IsNullOrEmpty(edge.Label)
                        ? $"        {edge.SourceId} {connector} {edge.TargetId}"
                        : $"        {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
                    mermaid.AppendLine(edgeLine);
                }
                mermaid.AppendLine("    end");
            }
        }

        // Add subflow connection edges
        foreach (var (subflowName, edge) in subflowConnections)
        {
            string connector = GetEdgeConnector(edge.Type);
            string edgeLine = string.IsNullOrEmpty(edge.Label)
                ? $"    {edge.SourceId} {connector} {edge.TargetId}"
                : $"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}";
            mermaid.AppendLine($"    %% Branch to {subflowName}");
            mermaid.AppendLine(edgeLine);
        }

        // Add styling for exception flows
        mermaid.AppendLine("    classDef exception fill:#f9c,stroke:#333,stroke-width:2px;");
        foreach (var subflow in subflows.Where(f => f.FlowType == FlowType.Exception))
        {
            mermaid.AppendLine($"    class {subflow.Name}_start_1 exception;");
        }

        return mermaid.ToString();
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
        if (string.IsNullOrEmpty(name))
            return name;

        if (name.StartsWith("error_"))
            name = name.Substring(6);

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
