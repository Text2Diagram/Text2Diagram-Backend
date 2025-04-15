using LangChain.Providers.Ollama;
using System.Text;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Flowchart;

/// <summary>
/// Generates flowchart diagrams in Mermaid.js format from structured data
/// extracted from use case specifications or natural language text.
/// </summary>
public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<FlowchartDiagramGenerator> logger;
    private readonly IAnalyzer<FlowchartDiagram> analyzer;
    private readonly ISyntaxValidator syntaxValidator;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        OllamaProvider provider,
        IConfiguration configuration,
        IAnalyzer<FlowchartDiagram> analyzer,
        ISyntaxValidator syntaxValidator)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
        this.analyzer = analyzer;
        this.syntaxValidator = syntaxValidator;
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
            string openingShape = GetNodeOpeningShape(node.Type);
            string closingShape = GetNodeClosingShape(node.Type, node.Label);
            mermaid.AppendLine($"    {node.Id}{openingShape}{closingShape}");
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
                    string openingShape = GetNodeOpeningShape(node.Type);
                    string closingShape = GetNodeClosingShape(node.Type, node.Label);
                    mermaid.AppendLine($"        {node.Id}{openingShape}{closingShape}");
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

    /// <summary>
    /// Gets the opening shape syntax for a specific node type in Mermaid using the modern @{shape} syntax.
    /// </summary>
    private string GetNodeOpeningShape(NodeType nodeType)
    {
        return nodeType switch
        {
            // Terminal points
            NodeType.Start => "@{shape: stadium, ",
            NodeType.End => "@{shape: stadium, ",

            // Basic nodes
            NodeType.Process => "@{shape: rect, ",
            NodeType.Decision => "@{shape: diam, ",

            // Input/Output nodes
            NodeType.Input => "@{shape: lean-r, ",
            NodeType.Output => "@{shape: lean-l, ",
            NodeType.Display => "@{shape: curv-trap, ",
            NodeType.Document => "@{shape: doc, ",
            NodeType.MultiDocument => "@{shape: docs, ",
            NodeType.File => "@{shape: flag, ",

            // Processing nodes
            NodeType.Preparation => "@{shape: hex, ",
            NodeType.ManualInput => "@{shape: sl-rect, ",
            NodeType.ManualOperation => "@{shape: trap-t, ",
            NodeType.PredefinedProcess => "@{shape: fr-rect, ",
            NodeType.UserDefinedProcess => "@{shape: tag-rect, ",
            NodeType.DividedProcess => "@{shape: div-rect, ",

            // Storage nodes
            NodeType.Database => "@{shape: cyl, ",
            NodeType.DirectAccessStorage => "@{shape: h-cyl, ",
            NodeType.DiskStorage => "@{shape: lin-cyl, ",
            NodeType.StoredData => "@{shape: bow-rect, ",
            NodeType.ExternalStorage => "@{shape: flip-tri, ",
            NodeType.Internal => "@{shape: win-pane, ",

            // Flow control
            NodeType.Connector => "@{shape: sm-circ, ",
            NodeType.OffPageConnector => "@{shape: tag-doc, ",
            NodeType.Delay => "@{shape: delay, ",
            NodeType.Loop => "@{shape: fork, ",
            NodeType.LoopLimit => "@{shape: notch-pent, ",

            // Junction nodes
            NodeType.Merge => "@{shape: tri, ",
            NodeType.Or => "@{shape: f-circ, ",
            NodeType.SummingJunction => "@{shape: cross-circ, ",
            NodeType.Sort => "@{shape: trap-b, ",
            NodeType.Collate => "@{shape: hourglass, ",

            // Annotation nodes
            NodeType.Card => "@{shape: notch-rect, ",
            NodeType.Comment => "@{shape: brace, ",
            NodeType.CommentRight => "@{shape: brace-r, ",
            NodeType.Comments => "@{shape: braces, ",
            NodeType.ComLink => "@{shape: bolt, ",

            // Default fallback
            _ => "@{shape: rect, "
        };
    }

    /// <summary>
    /// Gets the closing shape syntax for modern @{shape} syntax.
    /// For all node types using modern syntax, this is always "]".
    /// </summary>
    private string GetNodeClosingShape(NodeType nodeType, string nodeLabel)
    {
        return "label: \"" + nodeLabel + "\"}";
    }

    /// <summary>
    /// Gets the connector syntax for a specific edge type in Mermaid.
    /// </summary>
    private string GetEdgeConnector(EdgeType edgeType)
    {
        return edgeType switch
        {
            // Basic connections
            EdgeType.Normal => "-->",

            // Line styles
            EdgeType.Thick => "==>",
            EdgeType.Dotted => "-.->",

            // Semantic types
            EdgeType.Success => "-->",
            EdgeType.Failure => "-.->",
            EdgeType.Conditional => "-->",
            EdgeType.Return => "==>",

            // Special connections
            EdgeType.NoArrow => "---",
            EdgeType.OpenLink => "--o",
            EdgeType.CrossLink => "--x",
            EdgeType.CircleEnd => "---o",
            EdgeType.CrossEnd => "---x",

            // Combinations
            EdgeType.DottedNoArrow => "-.-",
            EdgeType.DottedOpenLink => "-.o",
            EdgeType.DottedCrossLink => "-.x",

            EdgeType.ThickNoArrow => "===",
            EdgeType.ThickOpenLink => "==o",
            EdgeType.ThickCrossLink => "==x",

            // Default fallback
            _ => "-->"
        };
    }
}
