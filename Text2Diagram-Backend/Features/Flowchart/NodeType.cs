namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Node types for flowchart diagrams using the modern @{shape} syntax from Mermaid v11.3.0+.
/// Each type represents a semantically distinct element in a flowchart.
/// </summary>
public enum NodeType
{
    // Terminal points
    Start,               // Rounded rectangle: ([Start])
    End,                 // Rounded rectangle: ([End])

    // Basic nodes
    Process,             // Rectangle: [Process]
    Subroutine,          // Double rectangle: [[Subroutine]]

    // Decisions
    Decision,            // Diamond: {Decision}

    // Inputs/Outputs
    InputOutput,         // Parallelogram: [/Input or Output/]

    // Document & Display
    Document,            // Parallelogram variant: [/Document/]
    DataStore,           // Cylinder: [("Data Store")]

    // Loops and Flow control
    Loop,                // Styled like process
    Parallel,            // Label only; no specific shape

    // Annotations
    Comment              // Note/Comment
}