using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Text2Diagram_Backend.Features.Flowchart;

public class TerminalNodesExtractor
{
    private readonly Kernel _kernel;
    private readonly ILogger<TerminalNodesExtractor> _logger;

    public TerminalNodesExtractor(Kernel kernel, ILogger<TerminalNodesExtractor> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<List<FlowNode>> ExtractTerminalNodesAsync(string useCaseSpec)
    {
        var prompt = GetPrompt(useCaseSpec);
        IChatCompletionService chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: _kernel);
        var textContent = response.Content ?? string.Empty;

        string jsonResult = string.Empty;
        var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (codeFenceMatch.Success)
        {
            jsonResult = codeFenceMatch.Groups[1].Value.Trim();
        }
        else
        {
            _logger.LogError("No valid JSON found in the response.");
            throw new InvalidOperationException("No valid JSON found in the response.");
        }

        var jsonNode = JsonNode.Parse(jsonResult);
        if (jsonNode == null)
        {
            throw new InvalidOperationException("Failed to parse JSON response from the model.");
        }

        if (jsonNode is not JsonArray jsonArray)
        {
            _logger.LogError("JSON response is not an array.");
            throw new InvalidOperationException("JSON response is not an array.");
        }

        var nodes = new List<FlowNode>();
        foreach (var node in jsonArray)
        {
            if (node == null) continue;

            var id = node["Id"]?.ToString();
            var label = node["Label"]?.ToString();
            var type = node["Type"]?.ToString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(type))
            {
                _logger.LogWarning("Invalid node data: Id, Label, or Type is missing or empty.");
                continue;
            }

            if (type == "Start" || type == "End")
            {
                nodes.Add(new FlowNode()
                {
                    Id = id,
                    Label = label,
                    Type = Enum.Parse<NodeType>(type)
                });
            }
        }

        return nodes;
    }

    private string GetPrompt(string useCaseSpec)
    {
        return $"""
            You are an expert Flowchart Analyzer that extracts and structures use case specifications into flowchart diagram components.
            Analyze the following use case specification and generate the start and end nodes of the flowchart.
            The output should be a JSON array of objects, each representing a terminal node with the following properties:
            - Id: A unique identifier for the node.
            - Label: A descriptive label for the node (e.g., "Start", "End").
            - Type: The type of node, which should be either "Start" or "End".
            Ensure that the output is a valid JSON array and that each node has a unique Id.
            Use the following use case specification as input:
            {useCaseSpec}
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            Use Case: Purchase  
            Actor: User  
            Basic Flow:
            1. The user clicks the "Checkout" button.
            2. The system asks for payment method.
            3. The user enters credit card details.
            4. The system validates the payment.
            5. The system displays order confirmation.

            Exception Flows:
            1. Invalid Payment:
               - The system shows an error message.
               - The system requests a different payment method.

            OUTPUT:
            ```json
            [
                {"Id": "start_1", "Label": "User clicks 'Checkout' button", "Type": "Start"},
                {"Id": "end_1", "Label": "Order confirmation displayed", "Type": "End"}
            ]
            ```
            """;
    }
}