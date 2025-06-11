using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using Text2Diagram_Backend.Common.Utils;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

public class BasicFlowExtractor
{
    private readonly Kernel _kernel;
    private readonly ILogger<BasicFlowExtractor> _logger;
    private IChatCompletionService _chatCompletionService;

    public BasicFlowExtractor(
        Kernel kernel,
        ILogger<BasicFlowExtractor> logger)
    {
        _kernel = kernel;
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    public async Task<BasicFlow> ExtractBasicFlowAsync(string basicFlowDescription)
    {
        var nodes = await ExtractNodesAsync(basicFlowDescription);
        var edges = await ExtractEdgesAsync(nodes);
        return new BasicFlow(nodes, edges);
    }

    private async Task<List<FlowNode>> ExtractNodesAsync(string basicFlowDescription)
    {
        var prompt = $"""
            You are an expert Flowchart Analyzer.
            Analyze the following flow description and generate all nodes of the flowchart, including Start, End, Process, Decision, InputOutput, Subroutine, Document, DataStore, Loop, Parallel, and Comment nodes.
            The output should be a JSON array of objects, each representing a node with the following properties:
            - Id: A unique identifier for the node (e.g., 'start_1', 'process_2').
            - Label: A descriptive label for the node, summarizing the action or condition.
            - Type: One of: Start, End, Process, Decision, InputOutput, Subroutine, Document, DataStore, Loop, Parallel, Comment.

            ### Rules for Node Types:
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

            Ensure that:
            - The output is a valid JSON array.
            - Each node has a unique Id.
            - There is exactly one Start node and at least one End node.
            - Node types are chosen based on the rules above and the context of the step.
            Use the following use case specification as input:
            {basicFlowDescription}
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            1. The user clicks the "Checkout" button.
            2. The system asks for payment method.
            3. The user enters credit card details.
            4. The system validates the payment.
            5. If the payment is valid, the system displays order confirmation.
            6. If the payment is invalid, the system displays an error message.
            7. Note: Payment validation may take up to 5 seconds.

            OUTPUT:
            ```json
            [
                {"Id": "start_1", "Label": "User clicks 'Checkout' button", "Type": "Start"},
                {"Id": "process_1", "Label": "Ask for payment method", "Type": "Process"},
                {"Id": "input_1", "Label": "Enter credit card details", "Type": "InputOutput"},
                {"Id": "decision_1", "Label": "Is payment valid?", "Type": "Decision"},
                {"Id": "end_1", "Label": "Order confirmation displayed", "Type": "End"},
                {"Id": "output_1", "Label": "Display error message", "Type": "InputOutput"},
                {"Id": "comment_1", "Label": "Note: Payment validation may take 5 seconds", "Type": "Comment"}
            ]
            ```
            """;

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;

        

        var nodes = Helpers.Flowchart.ExtractNodes(textContent);

        if (!nodes.Any(n => n.Type == NodeType.Start))
        {
            _logger.LogError("Flowchart must contain exactly one Start node.");
            throw new InvalidOperationException("Flowchart must contain exactly one Start node.");
        }

        if (!nodes.Any(n => n.Type == NodeType.End))
        {
            _logger.LogError("Flowchart must contain at least one End node.");
            throw new InvalidOperationException("Flowchart must contain at least one End node.");
        }

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
        if (nodeIds.Count != nodes.Count)
        {
            _logger.LogError("Duplicate node IDs found.");
            throw new InvalidOperationException("Duplicate node IDs found.");
        }

        return nodes;
    }

    private async Task<List<FlowEdge>> ExtractEdgesAsync(List<FlowNode> nodes)
    {
        var prompt = $"""
            You are an expert Flowchart Analyzer.
            Analyze the following nodes in a flowchart and generate valid edges for these nodes.
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
            Use the following nodes as input:
            {JsonSerializer.Serialize(nodes)}
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            [
                {"Id": "start_1", "Label": "User clicks 'Checkout' button", "Type": "Start"},
                {"Id": "process_1", "Label": "Ask for payment method", "Type": "Process"},
                {"Id": "input_1", "Label": "Enter credit card details", "Type": "InputOutput"},
                {"Id": "decision_1", "Label": "Is payment valid?", "Type": "Decision"},
                {"Id": "end_1", "Label": "Order confirmation displayed", "Type": "End"},
                {"Id": "output_1", "Label": "Display error message", "Type": "InputOutput"}
            ]
            OUTPUT:
            ```json
            [
                {"SourceId": "start_1", "TargetId": "process_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "process_1", "TargetId": "input_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "input_1", "TargetId": "decision_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "decision_1", "TargetId": "end_1", "Type": "Arrow", "Label": "Yes"},
                {"SourceId": "decision_1", "TargetId": "output_1", "Type": "Arrow", "Label": "No"}
            ]
            ```
            """;

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;


        var edges = Helpers.Flowchart.ExtractEdges(textContent);

        return edges;
    }

    

    
}