namespace Text2Diagram_Backend.Features.Flowchart;

public static class Prompts
{
    public const string NodeRules = """
    The output should be a JSON array of objects, each representing a node with the following properties:
    - Id: A unique identifier for the node (e.g., 'start_1', 'process_2').
    - Label: A descriptive label for the node, summarizing the action or condition.
    - Type: One of: Start, End, Process, Decision, InputOutput, Subroutine, Document, DataStore, Loop, Parallel, Comment.
    Rules:
    - Start: Use for the first step that initiates the process (e.g., "The user clicks the 'Checkout' button", "The process begins").
    - End: Use for the final step or outcome of the process (e.g., "The system displays order confirmation", "The process ends").
    - Process: Use for general processing steps that do not involve input/output, decisions, or storage (e.g., "The system calculates the total").
    - Decision: Use for steps that involve a condition or question with multiple outcomes (e.g., "Is the payment valid?", "Is the user logged in?").
    - InputOutput: Use for steps where data is input by the user or output to the user/system (e.g., "The user enters credit card details", "The system displays an error message").
    - Subroutine: Use for steps that call a separate process or module (e.g., "Call payment gateway API", "Process user authentication").
    - Document: Use for steps that generate or use a document (e.g., "The system generates a receipt", "Print an invoice").
    - DataStore: Use for steps involving data storage or retrieval from a database (e.g., "Save order details to database", "Retrieve user profile").
    - Loop: Use for steps that involve repetition until a condition is met (e.g., "Retry payment up to 3 times", "Repeat until valid input").
    - Parallel: Use for steps where multiple actions occur simultaneously (e.g., "Send email and update inventory").
    - Comment: Use for notes or explanations that provide context but are not part of the main flow (e.g., "Note: Payment validation may take 5 seconds").
    - Each node has a unique Id.
    - There is exactly one Start node and at least one End node.
    """;


    public const string EdgeRules = """
    The output should be a JSON array of objects, each representing an edge with the following properties:
    - SourceId: The Id of the source node.
    - TargetId: The Id of the target node.
    - Type: The type of edge. The type must be one of: Arrow, OpenArrow, CrossArrow, NoArrow.
    - Label: An optional label for the edge (e.g., "Yes" or "No" for Decision nodes).
    Ensure that the output is a valid JSON array and that each edge connects existing nodes.
    Pay special attention to:
    - Decision nodes: Create edges with labels like "Yes" or "No" for branches.
    - Loop nodes: Create edges that loop back to previous nodes.
    - Parallel nodes: Create multiple edges to represent parallel paths.  
    """;
}
