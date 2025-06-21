//using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.ChatCompletion;
//using System.Text.Json;
//using Text2Diagram_Backend.Features.Flowchart.Components;

//namespace Text2Diagram_Backend.Features.Flowchart.Agents;

//public class ExceptionFlowExtractor
//{
//    private readonly Kernel _kernel;
//    private readonly ILogger<ExceptionFlowExtractor> _logger;
//    private readonly IChatCompletionService _chatCompletionService;

//    public ExceptionFlowExtractor(Kernel kernel, ILogger<ExceptionFlowExtractor> logger)
//    {
//        _kernel = kernel;
//        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
//        _logger = logger;
//    }

//    public async Task<Flow> ExtractExceptionFlowAsync(string exceptionFlowDescription, string flowId)
//    {
//        var nodes = await ExtractNodesAsync(exceptionFlowDescription);
//        var edges = await ExtractEdgesAsync(nodes, exceptionFlowDescription);
//        return new Flow(flowId, nodes, edges);
//    }

//    private async Task<List<FlowNode>> ExtractNodesAsync(string exceptionFlowDescription)
//    {
//        var prompt = $"""
//            You are an expert Flowchart Analyzer for an e-commerce purchase process.
//            Analyze the following exception flow description from a use case for purchasing items online.
//            An exception flow represents an error condition that prevents the normal purchase process (e.g., out-of-stock items, invalid quantities).

//            {Prompts.NodeRules}

//            ### Context:
//            - The use case involves a user purchasing items from a shopping cart or product detail page.
//            - Exception flows typically involve error conditions like disabled actions (e.g., unclickable checkboxes) or invalid inputs (e.g., unselected options, invalid quantities).
//            - The flow usually terminates with an error message or returns to a previous step.
//            - Postconditions include preventing invalid purchases (e.g., out-of-stock items, invalid quantities).

//            Ensure that:
//            - Nodes reflect the error condition (e.g., InputOutput for error messages like 'Product out of stock').
//            - The Start node represents the point where the exception occurs (e.g., 'User attempts to select item').
//            - The End node represents termination (e.g., 'Process terminated') or a return to a previous step.
//            - Include a Decision node for checking conditions (e.g., 'Is product in stock?', 'Is quantity valid?').
//            Use the following exception flow description:
//            {exceptionFlowDescription}

//            ### EXAMPLE:
//            INPUT:
//            1. The user cannot click the checkbox for a product that is out of stock or removed by the seller.
//            2. The system displays an error message.

//            OUTPUT:
//            ```json
//            [
//                {{"Id": "start_1", "Label": "User attempts to select item", "Type": "Start"}},
//                {{"Id": "decision_1", "Label": "Is product in stock?", "Type": "Decision"}},
//                {{"Id": "output_1", "Label": "Display error: Product out of stock", "Type": "InputOutput"}},
//                {{"Id": "end_1", "Label": "Process terminated", "Type": "End"}}
//            ]
//            """;

//        var chatHistory = new ChatHistory();
//        chatHistory.AddUserMessage(prompt);
//        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
//        var textContent = response.Content ?? string.Empty;

//        var nodes = FlowchartHelpers.ExtractNodes(textContent);

//        if (!nodes.Any(n => n.Type == NodeType.Start))
//        {
//            _logger.LogError("Exception flow must contain exactly one Start node.");
//            throw new InvalidOperationException("Exception flow must contain exactly one Start node.");
//        }

//        if (!nodes.Any(n => n.Type == NodeType.End))
//        {
//            _logger.LogError("Exception flow must contain at least one End node.");
//            throw new InvalidOperationException("Exception flow must contain at least one End node.");
//        }

//        var nodeIds = nodes.Select(n => n.Id).ToHashSet();
//        if (nodeIds.Count != nodes.Count)
//        {
//            _logger.LogError("Duplicate node IDs found in exception flow.");
//            throw new InvalidOperationException("Duplicate node IDs found in exception flow.");
//        }

//        return nodes;
//    }

//    private async Task<List<flowedge>> ExtractEdgesAsync(List<flownode> nodes, string exceptionFlowDescription)
//    {
//        var prompt = $"""
//You are an expert Flowchart Analyzer for an e-commerce purchase process.
//Analyze the following nodes and exception flow description to generate valid edges.</flownode></flowedge>

//{Prompts.EdgeRules}

//Context:
//The exception flow represents an error condition in the purchase process.
//Edges should reflect the sequence leading to the error and its resolution (e.g., displaying an error message).
//Use CrossArrow for edges leading to termination (e.g., 'Process terminated').
//Decision nodes branch to error paths (e.g., 'No' to an error message).
//Ensure that:

//Edges follow the sequence described in the exception flow.
//Termination paths use CrossArrow to indicate failure.
//Error messages are connected to End nodes or return to previous steps. Nodes: {JsonSerializer.Serialize(nodes)} Exception flow: {exceptionFlowDescription}
//EXAMPLE:
//INPUT:
//Nodes: [
//{{"Id": "start_1", "Label": "User attempts to select item", "Type": "Start"}},
//{{"Id": "decision_1", "Label": "Is product in stock?", "Type": "Decision"}},
//{{"Id": "output_1", "Label": "Display error: Product out of stock", "Type": "InputOutput"}},
//{{"Id": "end_1", "Label": "Process terminated", "Type": "End"}}
//]
//OUTPUT:
//[
//    {{"SourceId": "start_1", "TargetId": "decision_1", "Type": "Arrow", "Label": ""}},
//    {{"SourceId": "decision_1", "TargetId": "output_1", "Type": "CrossArrow", "Label": "No"}},
//    {{"SourceId": "output_1", "TargetId": "end_1", "Type": "CrossArrow", "Label": ""}}
//]
//""";

//        var chatHistory = new ChatHistory();
//        chatHistory.AddUserMessage(prompt);
//        var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
//        var textContent = response.Content ?? string.Empty;

//        var edges = FlowchartHelpers.ExtractEdges(textContent);

//        return edges;
//    }
//}