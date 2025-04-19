namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Types of edges (connections) between nodes in a flowchart.
/// Corresponds to different arrow styles in Mermaid.js.
/// </summary>
public enum EdgeType
{
    // Basic connections
    Normal,             // Regular arrow -->

    // Line styles
    Thick,              // Thick line ==>
    Dotted,             // Dotted line -..->

    // Semantic types
    Success,            // Success path (normal arrow)
    Failure,            // Failure path (dotted arrow)
    Conditional,        // Conditional path
    Return,             // Return path (thick arrow)

    // Special connections
    NoArrow,            // Line without arrow ---
    OpenLink,           // Link with open arrow --o
    CrossLink,          // Link with a cross --x
    CircleEnd,          // Circle on the end ---o
    CrossEnd,           // Cross on the end ---x

    // Combinations
    DottedNoArrow,      // Dotted line without arrow -..-
    DottedOpenLink,     // Dotted line with open arrow -..o
    DottedCrossLink,    // Dotted line with cross -..x

    ThickNoArrow,       // Thick line without arrow ===
    ThickOpenLink,      // Thick line with open arrow ==o
    ThickCrossLink      // Thick line with cross ==x
}