using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class AlternativeFlowExtractor
{
    private readonly ILLMService _llmService;
    private readonly ILogger<AlternativeFlowExtractor> _logger;

    public AlternativeFlowExtractor(ILLMService llmService, ILogger<AlternativeFlowExtractor> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<Flow> ExtractAlternativeFlowAsync(string alternativeFlowDescription, string flowName)
    {
        var nodes = await ExtractNodesAsync(alternativeFlowDescription);
        var edges = await ExtractEdgesAsync(nodes, alternativeFlowDescription);
        return new Flow(flowName, FlowType.Alternative, nodes, edges);
    }

    private async Task<List<FlowNode>> ExtractNodesAsync(string alternativeFlowDescription)
    {
        var prompt = $"""
            You are an expert FlowchartDiagram Analyzer.
            {Prompts.NodeRules}
            Analyze the following alternative flow description:
            {alternativeFlowDescription}
            Ensure that:
            - Nodes reflect the alternative nature of the flow.
            - The Start node represents the entry point of the alternative flow.
            - The End node connects to the main flow or completes the alternative path.
            - Include a Subroutine node for complex steps.  
            - The node's Id must start with alternative_flow_ prefix (e.g., alternative_flow_start_1)
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            1. The user views the product detail page.
            2. The user clicks the 'Buy Now' button.
            3. If the product has multiple options, the user selects one available option.
            4. The user adjusts the quantity.
            5. The user clicks the 'Checkout' button.
            6. The system processes the checkout.
            7. The user is redirected to an order confirmation page.

            OUTPUT:
            ```json
            [
                {"Id": "alternative_flow_start_1", "Label": "User views product detail page", "Type": "Start"},
                {"Id": "alternative_flow_process_1", "Label": "User clicks 'Buy Now'", "Type": "Process"},
                {"Id": "alternative_flow_decision_1", "Label": "Product has multiple options?", "Type": "Decision"},
                {"Id": "alternative_flow_input_1", "Label": "User selects option", "Type": "InputOutput"},
                {"Id": "alternative_flow_input_2", "Label": "User adjusts quantity", "Type": "InputOutput"},
                {"Id": "alternative_flow_process_2", "Label": "User clicks 'Checkout'", "Type": "Process"},
                {"Id": "alternative_flow_subroutine_1", "Label": "System processes checkout", "Type": "Subroutine"},
                {"Id": "alternative_flow_end_1", "Label": "Redirect to order confirmation", "Type": "End"}
            ]
            """;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;

        var nodes = FlowchartHelpers.ExtractNodes(textContent);

        if (nodes.Where(n => n.Type == NodeType.Start).Count() != 1)
        {
            _logger.LogError("Alternative flow must contain exactly one Start node.");
            throw new InvalidOperationException("Alternative flow must contain exactly one Start node.");
        }

        if (!nodes.Any(n => n.Type == NodeType.End))
        {
            _logger.LogError("Alternative flow must contain at least one End node.");
            throw new InvalidOperationException("Alternative flow must contain at least one End node.");
        }

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
        if (nodeIds.Count != nodes.Count)
        {
            _logger.LogError("Duplicate node IDs found in alternative flow.");
            throw new InvalidOperationException("Duplicate node IDs found in alternative flow.");
        }

        return nodes;
    }

    private async Task<List<FlowEdge>> ExtractEdgesAsync(List<FlowNode> nodes, string alternativeFlowDescription)
    {
        var prompt = $"""
        You are an expert FlowchartDiagram Analyzer.
        Analyze the following nodes and alternative flow description to generate valid edges.
        {Prompts.EdgeRules}
        Context:
        - Edges should reflect the sequence of steps in the alternative flow.
        - Decision nodes may branch to different paths (e.g., selecting product options).
        - The flow typically connects to the main flow at the checkout process or order confirmation.
        - Use Arrow for normal transitions and OpenArrow for optional paths (e.g., skipping option selection).
        Ensure that:
        - Edges follow the sequence described in the alternative flow.
        - Decision nodes have at least two edges with appropriate labels (e.g., 'Yes'/'No').
        - The final edge connects to a shared node in the main flow. Nodes: {JsonSerializer.Serialize(nodes)} Alternative flow: {alternativeFlowDescription}
        """
        +
        """
        EXAMPLE:
        INPUT:
        Nodes: [
        {"Id": "alternative_flow_start_1", "Label": "User views product detail page", "Type": "Start"},
        {"Id": "alternative_flow_process_1", "Label": "User clicks 'Buy Now'", "Type": "Process"},
        {"Id": "alternative_flow_decision_1", "Label": "Product has multiple options?", "Type": "Decision"},
        {"Id": "alternative_flow_input_1", "Label": "User selects option", "Type": "InputOutput"},
        {"Id": "alternative_flow_input_2", "Label": "User adjusts quantity", "Type": "InputOutput"},
        {"Id": "alternative_flow_process_2", "Label": "User clicks 'Checkout'", "Type": "Process"},
        {"Id": "alternative_flow_subroutine_1", "Label": "System processes checkout", "Type": "Subroutine"},
        {"Id": "alternative_flow_end_1", "Label": "Redirect to order confirmation", "Type": "End"}
        ]
        OUTPUT:
        [
            {"SourceId": "alternative_flow_start_1", "TargetId": "alternative_flow_process_1", "Type": "Arrow", "Label": ""},
            {"SourceId": "alternative_flow_process_1", "TargetId": "alternative_flow_decision_1", "Type": "Arrow", "Label": ""},
            {"SourceId": "alternative_flow_decision_1", "TargetId": "alternative_flow_input_1", "Type": "Arrow", "Label": "Yes"},
            {"SourceId": "alternative_flow_input_1", "TargetId": "alternative_flow_input_2", "Type": "Arrow", "Label": ""},
            {"SourceId": "alternative_flow_decision_1", "TargetId": "alternative_flow_input_2", "Type": "OpenArrow", "Label": "No"},
            {"SourceId": "alternative_flow_input_2", "TargetId": "alternative_flow_process_2", "Type": "Arrow", "Label": ""},
            {"SourceId": "alternative_flow_process_2", "TargetId": "alternative_flow_subroutine_1", "Type": "Arrow", "Label": ""},
            {"SourceId": "alternative_flow_subroutine_1", "TargetId": "alternative_flow_end_1", "Type": "Arrow", "Label": ""}
        ]
        """;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;

        var edges = FlowchartHelpers.ExtractEdges(textContent);

        return edges;
    }
}