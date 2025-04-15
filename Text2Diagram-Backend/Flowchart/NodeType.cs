namespace Text2Diagram_Backend.Flowchart;

/// <summary>
/// Node types for flowchart diagrams using the modern @{shape} syntax from Mermaid v11.3.0+.
/// Each type represents a semantically distinct element in a flowchart.
/// </summary>
public enum NodeType
{
    // Terminal points
    Start,                // Beginning of process flow (rounded rectangle)
    End,                  // End of process flow (rounded rectangle)
    
    // Basic nodes
    Process,              // Standard process step (rectangle)
    Decision,             // Decision point (diamond)
    
    // Input/Output nodes
    Input,                // Data input (parallelogram leaning right)
    Output,               // Data output (parallelogram leaning left)
    Display,              // Display information (curved trapezoid)
    Document,             // Single document (document shape)
    MultiDocument,        // Multiple documents (stacked documents)
    File,                 // File (folder shape)
    
    // Processing nodes
    Preparation,          // Preparation step (hexagon)
    ManualInput,          // Manual input (trapezoid)
    ManualOperation,      // Manual operation (trapezoid with expanded bottom)
    PredefinedProcess,    // Predefined process (subroutine)
    UserDefinedProcess,   // User-defined process (rectangle with vertical bars)
    DividedProcess,       // Process with divisions (divided rectangle)
    
    // Storage nodes
    Database,             // Database (cylinder)
    DirectAccessStorage,  // Direct access storage (horizontal cylinder)
    DiskStorage,          // Disk storage (lined cylinder)
    StoredData,           // Stored data/tape (tape shape)
    ExternalStorage,      // External storage (rectangular drum)
    Internal,             // Internal storage (rectangle with double top)
    
    // Flow control
    Connector,            // Connection point (small circle)
    OffPageConnector,     // Off-page connector (home plate shape)
    Delay,                // Delay/wait (semi-circle/delay)
    Loop,                 // Loop (loop shape)
    LoopLimit,            // Loop limit (loop with expanded top)
    
    // Junction nodes
    Merge,                // Merge paths (inverted triangle)
    Or,                   // OR junction (circle with 'or')
    SummingJunction,      // Summing junction (circle with plus)
    Sort,                 // Sort junction (sort symbol)
    Collate,              // Collate (hourglass)
    
    // Annotation nodes
    Card,                 // Information card (notched rectangle)
    Comment,              // Left-side comment (left curly brace)
    CommentRight,         // Right-side comment (right curly brace)
    Comments,             // Two-sided comment (curly braces on both sides)
    ComLink               // Communication link (lightning bolt)
}