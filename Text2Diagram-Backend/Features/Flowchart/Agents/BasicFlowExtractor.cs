
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class BasicFlowExtractor
{
    private readonly ILLMService _llmService;
    private readonly ILogger<BasicFlowExtractor> _logger;

    public BasicFlowExtractor(
        ILLMService llmService,
        ILogger<BasicFlowExtractor> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<Flow> ExtractBasicFlowAsync(string basicFlowDescription)
    {
        var nodes = await ExtractNodesAsync(basicFlowDescription);
        var edges = await ExtractEdgesAsync(nodes, basicFlowDescription);
        return new Flow("", nodes, edges);
    }

    private async Task<List<FlowNode>> ExtractNodesAsync(string basicFlowDescription)
    {
        var prompt = $"""
            You are an expert Flowchart Analyzer.
            {Prompts.NodeRules}
            Analyze the following basic flow description:
            {basicFlowDescription}
            """
            +
            """
            Ensure that:
            - Nodes cover all steps in the basic flow.
            - The Start node is the entry point.
            - The End node reflects the final outcome.
            - Include a Subroutine node for complex steps.
            ### EXAMPLE:
            INPUT:
            1. The user is on the shopping cart page and has added items to the cart.
            2. The user selects items for checkout by clicking the checkbox before each item.
            3. The system displays the summary of costs.
            4. The user clicks the 'Checkout' button.
            5. The system processes the checkout.
            6. The user is redirected to a page showing one or more new orders for the selected items.
               - Products from different shops are grouped into separate orders.
               - Products from the same shop are grouped into a single order.

            OUTPUT:
            ```json
            [
                {"Id": "start_1", "Label": "User views shopping cart", "Type": "Start"},
                {"Id": "input_1", "Label": "User selects items via checkboxes", "Type": "InputOutput"},
                {"Id": "output_1", "Label": "System displays cost summary", "Type": "InputOutput"},
                {"Id": "process_1", "Label": "User clicks 'Checkout'", "Type": "Process"},
                {"Id": "subroutine_1", "Label": "System processes checkout", "Type": "Subroutine"},
                {"Id": "subroutine_2", "Label": "Group items by shop", "Type": "Subroutine"},
                {"Id": "end_1", "Label": "Redirect to order confirmation", "Type": "End"}
            ]
            ```
            """;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;

        var nodes = FlowchartHelpers.ExtractNodes(textContent);

        if (nodes.Where(n => n.Type == NodeType.Start).Count() != 1)
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

    private async Task<List<FlowEdge>> ExtractEdgesAsync(List<FlowNode> nodes, string basicFlowDescription)
    {
        var prompt = $"""
            You are an expert Flowchart Analyzer.
            Analyze the following nodes and the basic flow in a flowchart, then generate valid edges.
            {Prompts.EdgeRules}
            Use the following nodes and basic flow as input:
            - Nodes: {JsonSerializer.Serialize(nodes)}
            - Basic flow: {basicFlowDescription}
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            [
                {"Id": "start_1", "Label": "User views shopping cart", "Type": "Start"},
                {"Id": "input_1", "Label": "User selects items via checkboxes", "Type": "InputOutput"},
                {"Id": "output_1", "Label": "System displays cost summary", "Type": "InputOutput"},
                {"Id": "process_1", "Label": "User clicks 'Checkout'", "Type": "Process"},
                {"Id": "subroutine_1", "Label": "System processes checkout", "Type": "Subroutine"},
                {"Id": "subroutine_2", "Label": "Group items by shop", "Type": "Subroutine"},
                {"Id": "end_1", "Label": "Redirect to order confirmation", "Type": "End"}
            ]
            OUTPUT:
            ```json
            [
                {"SourceId": "start_1", "TargetId": "input_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "input_1", "TargetId": "output_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "output_1", "TargetId": "process_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "process_1", "TargetId": "subroutine_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "subroutine_1", "TargetId": "subroutine_2", "Type": "Arrow", "Label": ""},
                {"SourceId": "subroutine_2", "TargetId": "end_1", "Type": "Arrow", "Label": ""}
            ]
            ```
            """;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;

        var edges = FlowchartHelpers.ExtractEdges(textContent);

        return edges;
    }


}