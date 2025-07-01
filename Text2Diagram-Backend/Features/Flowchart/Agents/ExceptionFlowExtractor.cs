using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class ExceptionFlowExtractor
{
    private readonly ILLMService _llmService;
    private readonly ILogger<ExceptionFlowExtractor> _logger;

    public ExceptionFlowExtractor(ILLMService llmService, ILogger<ExceptionFlowExtractor> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<Flow> ExtractExceptionFlowAsync(string exceptionFlowDescription, string flowName)
    {
        var nodes = await ExtractNodesAsync(exceptionFlowDescription);
        var edges = await ExtractEdgesAsync(nodes, exceptionFlowDescription);
        return new Flow(flowName, FlowType.Exception, nodes, edges);
    }

    private async Task<List<FlowNode>> ExtractNodesAsync(string exceptionFlowDescription)
    {
        var prompt = $"""
            You are an expert FlowchartDiagram Analyzer.
            {Prompts.NodeRules}
            Analyze the following exception flow description:
            {exceptionFlowDescription}
            Ensure that:
            - Nodes reflect the error nature of the flow (e.g., a Decision node for checking stock status).
            - The Start node represents the entry point of the exception flow (e.g., 'User attempts to select out-of-stock item').
            - The End node represents the error state (e.g., 'Checkbox disabled') or correction (e.g., 'User selects valid option').
            - Include a Display node for error messages (e.g., 'Show error message') or a Process node for system restrictions (e.g., 'Disable Checkout button').
            - The node's Id must start with exception_flow_ prefix (e.g., exception_flow_start_1)
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            1. The user cannot click the checkbox for a product that is out of stock or removed by the seller, even if it is in the shopping cart.

            OUTPUT:
            ```json
            [
                {"Id": "exception_flow_start_1", "Label": "User attempts to select item", "Type": "Start"},
                {"Id": "exception_flow_decision_1", "Label": "Item out of stock or removed?", "Type": "Decision"},
                {"Id": "exception_flow_process_1", "Label": "Disable checkbox", "Type": "Process"},
                {"Id": "exception_flow_end_1", "Label": "Selection prevented", "Type": "End"}
            ]
            """
            +
            """
            ### ANOTHER EXAMPLE:
            INPUT:
            2. When purchasing from the product detail page:
               - The user cannot purchase a product with multiple options without selecting one available option.
               - The user cannot purchase a product with a quantity exceeding the current stock or less than one.
               - The user cannot purchase a product with no stock or an out-of-stock option for products with multiple options.
               - The 'Checkout' button is disabled if the selected product is invalid.

            OUTPUT:
            ```json
            [
                {"Id": "exception_flow_start_1", "Label": "User attempts to purchase from product detail page", "Type": "Start"},
                {"Id": "exception_flow_decision_1", "Label": "Valid product selection?", "Type": "Decision"},
                {"Id": "exception_flow_display_1", "Label": "Show error message", "Type": "Display"},
                {"Id": "exception_flow_process_1", "Label": "Disable Checkout button", "Type": "Process"},
                {"Id": "exception_flow_input_1", "Label": "User corrects selection", "Type": "InputOutput"},
                {"Id": "exception_flow_end_1", "Label": "Return to main flow", "Type": "End"}
            ]
            """
            ;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;

        var nodes = FlowchartHelpers.ExtractNodes(textContent);

        if (nodes.Where(n => n.Type == NodeType.Start).Count() != 1)
        {
            _logger.LogError("Exception flow must contain exactly one Start node.");
            throw new InvalidOperationException("Exception flow must contain exactly one Start node.");
        }

        if (!nodes.Any(n => n.Type == NodeType.End))
        {
            _logger.LogError("Exception flow must contain at least one End node.");
            throw new InvalidOperationException("Exception flow must contain at least one End node.");
        }

        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
        if (nodeIds.Count != nodes.Count)
        {
            _logger.LogError("Duplicate node IDs found in exception flow.");
            throw new InvalidOperationException("Duplicate node IDs found in exception flow.");
        }

        return nodes;
    }

    private async Task<List<FlowEdge>> ExtractEdgesAsync(List<FlowNode> nodes, string exceptionFlowDescription)
    {
        var prompt = $"""
            You are an expert FlowchartDiagram Analyzer.
            Analyze the following nodes and exception flow description to generate valid edges.
            {Prompts.EdgeRules}
            ### Context:
            - Edges should reflect the sequence of steps in the exception flow, focusing on error conditions or restrictions.
            - Decision nodes may branch to error paths (e.g., invalid selection) or correction paths (e.g., user retries).
            - The flow typically ends with an error state (e.g., disabled action) or rejoins the main flow after correction.
            - Use Arrow for mandatory transitions and OpenArrow for optional paths (e.g., user correction).
            Ensure that:
            - Edges follow the sequence described in the exception flow.
            - Decision nodes have at least two edges with appropriate labels (e.g., 'Invalid'/'Valid').
            - The final edge leads to an error state or reconnects to the main flow.
            Nodes: {JsonSerializer.Serialize(nodes)}
            Exception flow: {exceptionFlowDescription}
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            Nodes: [
                {"Id": "exception_flow_start_1", "Label": "User attempts to select item", "Type": "Start"},
                {"Id": "exception_flow_decision_1", "Label": "Item out of stock or removed?", "Type": "Decision"},
                {"Id": "exception_flow_process_1", "Label": "Disable checkbox", "Type": "Process"},
                {"Id": "exception_flow_end_1", "Label": "Selection prevented", "Type": "End"}
            ]
            OUTPUT:
            ```json
            [
                {"SourceId": "exception_flow_start_1", "TargetId": "exception_flow_decision_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "exception_flow_decision_1", "TargetId": "exception_flow_process_1", "Type": "Arrow", "Label": "Yes"},
                {"SourceId": "exception_flow_decision_1", "TargetId": "exception_flow_end_1", "Type": "OpenArrow", "Label": "No"},
                {"SourceId": "exception_flow_process_1", "TargetId": "exception_flow_end_1", "Type": "Arrow", "Label": ""}
            ]
            """
            +
            """
            ### ANOTHER EXAMPLE:
            INPUT:
            Nodes: [
                {"Id": "exception_flow_start_1", "Label": "User attempts to purchase from product detail page", "Type": "Start"},
                {"Id": "exception_flow_decision_1", "Label": "Valid product selection?", "Type": "Decision"},
                {"Id": "exception_flow_display_1", "Label": "Show error message", "Type": "Display"},
                {"Id": "exception_flow_process_1", "Label": "Disable Checkout button", "Type": "Process"},
                {"Id": "exception_flow_input_1", "Label": "User corrects selection", "Type": "InputOutput"},
                {"Id": "exception_flow_end_1", "Label": "Return to main flow", "Type": "End"}
            ]
            OUTPUT:
            ```json
            [
                {"SourceId": "exception_flow_start_1", "TargetId": "exception_flow_decision_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "exception_flow_decision_1", "TargetId": "exception_flow_display_1", "Type": "Arrow", "Label": "Invalid"},
                {"SourceId": "exception_flow_decision_1", "TargetId": "exception_flow_end_1", "Type": "OpenArrow", "Label": "Valid"},
                {"SourceId": "exception_flow_display_1", "TargetId": "exception_flow_process_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "exception_flow_process_1", "TargetId": "exception_flow_input_1", "Type": "Arrow", "Label": ""},
                {"SourceId": "exception_flow_input_1", "TargetId": "exception_flow_end_1", "Type": "Arrow", "Label": ""}
            ]
            """
            ;

        var response = await _llmService.GenerateContentAsync(prompt);
        var textContent = response.Content ?? string.Empty;
        _logger.LogDebug("LLM response for edges:\n{0}", textContent);

        var edges = FlowchartHelpers.ExtractEdges(textContent);

        return edges;
    }
}