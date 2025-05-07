namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Types of edges (connections) between nodes in a flowchart.
/// Corresponds to different arrow styles in Mermaid.js.
/// </summary>
public enum EdgeType
{
    Arrow,              // Regular arrow --> 
    OpenArrow,          // Open arrow --o
    CrossArrow,         // Cross arrow --x
    NoArrow,            // Line without arrow ---
}