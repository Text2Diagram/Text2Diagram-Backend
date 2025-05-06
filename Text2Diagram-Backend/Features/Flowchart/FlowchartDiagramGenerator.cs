using System.Text;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Generates flowchart diagrams in Mermaid.js format from structured data
/// extracted from use case specifications or natural language text.
/// </summary>
public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<FlowchartDiagramGenerator> logger;
    private readonly UseCaseSpecAnalyzerForFlowchart analyzer;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        UseCaseSpecAnalyzerForFlowchart analyzer)
    {
        this.logger = logger;
        this.analyzer = analyzer;
    }

    /// <summary>
    /// Generates a flowchart diagram in Mermaid.js format from text input.
    /// </summary>
    /// <param name="input">Use case specifications, BPMN files, or natural language text.</param>
    /// <returns>Generated Mermaid code for Flowchart Diagram</returns>
    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeAsync(input);

            // Generate Mermaid syntax
            string mermaidCode = GenerateMermaidCode(diagram);

            logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", mermaidCode);

            // Validate and correct if needed
            return mermaidCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

    /// <summary>
    /// Generates Mermaid.js compatible syntax for the flowchart diagram.
    /// </summary>
    private string GenerateMermaidCode(FlowchartDiagram diagram)
    {
        var mermaid = new StringBuilder();

        // Start flowchart definition with direction
        mermaid.AppendLine("graph TD");

        // Generate node definitions
        foreach (var node in diagram.Nodes)
        {
            string nodeDef = GetNodeWrappedLabel(node.Id, node.Type, node.Label);
            mermaid.AppendLine($"    {nodeDef}");
        }

        mermaid.AppendLine();

        // Generate edge definitions
        foreach (var edge in diagram.Edges.Where(e => !IsDuplicateEdge(e, diagram.Subflows)))
        {
            string connector = GetEdgeConnector(edge.Type);
            if (!string.IsNullOrEmpty(edge.Label))
            {
                mermaid.AppendLine($"    {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}");
            }
            else
            {
                mermaid.AppendLine($"    {edge.SourceId} {connector} {edge.TargetId}");
            }
        }

        // Generate subgraphs
        if (diagram.Subflows != null && diagram.Subflows.Any())
        {
            foreach (var subflow in diagram.Subflows)
            {
                mermaid.AppendLine();
                mermaid.AppendLine($"    subgraph {subflow.Name}[\"{FormatSubflowName(subflow.Name)}\"]");

                // Add nodes in subflow
                foreach (var node in subflow.Nodes)
                {
                    string nodeDef = GetNodeWrappedLabel(node.Id, node.Type, node.Label);
                    mermaid.AppendLine($"        {nodeDef}");
                }

                // Add edges in subflow
                foreach (var edge in subflow.Edges)
                {
                    string connector = GetEdgeConnector(edge.Type);
                    if (!string.IsNullOrEmpty(edge.Label))
                    {
                        mermaid.AppendLine($"        {edge.SourceId} {connector}|{edge.Label}| {edge.TargetId}");
                    }
                    else
                    {
                        mermaid.AppendLine($"        {edge.SourceId} {connector} {edge.TargetId}");
                    }
                }

                mermaid.AppendLine("    end");
            }
        }


        return mermaid.ToString();
    }

    /// <summary>
    /// Checks if an edge is already represented in a subflow to avoid duplication.
    /// </summary>
    private bool IsDuplicateEdge(FlowEdge edge, List<Subflow> subflows)
    {
        if (subflows == null || !subflows.Any())
            return false;

        return subflows.Any(subflow =>
            subflow.Edges.Any(e =>
                e.SourceId == edge.SourceId && e.TargetId == edge.TargetId));
    }

    /// <summary>
    /// Formats a subflow name for display, converting underscores to spaces and capitalizing words.
    /// </summary>
    private string FormatSubflowName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Remove error_ prefix if present
        if (name.StartsWith("error_"))
            name = name.Substring(6);

        // Replace underscores with spaces and capitalize words
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

    /// <summary>
    /// Gets the connector syntax for a specific edge type in Mermaid.
    /// </summary>
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
