using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Analyzes structured use case specifications to extract elements for flowchart generation.
/// This analyzer is optimized for structured text following use case specification format.
/// </summary>
public class UseCaseSpecAnalyzerForFlowchart : IAnalyzer<FlowchartDiagram>
{
    private readonly Kernel kernel;
    private readonly ILogger<UseCaseSpecAnalyzerForFlowchart> logger;
    private const int MaxRetries = 3;
    private static readonly string[] ValidNodeTypes = Enum.GetNames(typeof(NodeType));
    private static readonly string[] ValidEdgeTypes = Enum.GetNames(typeof(EdgeType));

    public UseCaseSpecAnalyzerForFlowchart(Kernel kernel, ILogger<UseCaseSpecAnalyzerForFlowchart> logger)
    {
        this.kernel = kernel;
        this.logger = logger;
    }

    /// <summary>
    /// Analyzes a structured use case specification to generate a flowchart diagram.
    /// </summary>
    /// <param name="useCaseSpec">The use case specification text to analyze.</param>
    /// <returns>A flowchart diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the use case specification is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<FlowchartDiagram> AnalyzeAsync(string useCaseSpec)
    {
        if (string.IsNullOrWhiteSpace(useCaseSpec))
        {
            logger.LogError("Use case specification is empty or null.");
            throw new ArgumentException("Use case specification cannot be empty.", nameof(useCaseSpec));
        }

        string? errorMessage = null;
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var prompt = GetAnalysisPrompt(useCaseSpec, errorMessage);
                IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);

                var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                var textContent = response.Content ?? "";
                logger.LogInformation("Attempt {attempt}: Response: {response}", attempt, textContent);

                // Extract JSON from response
                string jsonResult;
                var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
                if (codeFenceMatch.Success)
                {
                    jsonResult = codeFenceMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // Fallback to raw JSON
                    var rawJsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}", RegexOptions.Singleline);
                    if (!rawJsonMatch.Success)
                    {
                        errorMessage = "No valid JSON found in response. Expected JSON in code fences (```json ... ```) or raw JSON object.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        continue;
                    }
                    jsonResult = rawJsonMatch.Value.Trim();
                }

                if (string.IsNullOrWhiteSpace(jsonResult))
                {
                    errorMessage = "Extracted JSON is empty.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                logger.LogInformation("Attempt {attempt}: Extracted JSON: {json}", attempt, jsonResult);

                // Validate JSON structure before deserialization
                var jsonNode = JsonNode.Parse(jsonResult);
                if (jsonNode == null)
                {
                    errorMessage = "Failed to parse JSON. Ensure the response is valid JSON.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                var nodes = jsonNode["Nodes"]?.AsArray();
                var edges = jsonNode["Edges"]?.AsArray();
                var subflows = jsonNode["Subflows"]?.AsArray();

                if (nodes == null || nodes.Count == 0)
                {
                    errorMessage = "Nodes array is missing or empty.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                // Validate nodes
                var nodeIds = new HashSet<string>();
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    var id = node["Id"]?.ToString();
                    var label = node["Label"]?.ToString();
                    var type = node["Type"]?.ToString();

                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(type))
                    {
                        errorMessage = "Node missing required fields: Id, Label, or Type.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    if (!ValidNodeTypes.Contains(type))
                    {
                        errorMessage = $"Invalid NodeType '{type}'. Valid types are: {string.Join(", ", ValidNodeTypes)}.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    if (!nodeIds.Add(id))
                    {
                        errorMessage = $"Duplicate node ID '{id}' found.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }
                }

                // Validate edges
                var edgeIds = new HashSet<string>();
                var allNodeIds = new HashSet<string>(nodeIds);
                if (edges != null)
                {
                    foreach (var edge in edges)
                    {
                        if (edge == null) continue;
                        var id = edge["Id"]?.ToString();
                        var sourceId = edge["SourceId"]?.ToString();
                        var targetId = edge["TargetId"]?.ToString();
                        var type = edge["Type"]?.ToString();

                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(type))
                        {
                            errorMessage = "Edge missing required fields: Id, SourceId, TargetId, or Type.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!ValidEdgeTypes.Contains(type))
                        {
                            errorMessage = $"Invalid EdgeType '{type}'. Valid types are: {string.Join(", ", ValidEdgeTypes)}.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!edgeIds.Add(id))
                        {
                            errorMessage = $"Duplicate edge ID '{id}' found.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!allNodeIds.Contains(sourceId) || !allNodeIds.Contains(targetId))
                        {
                            errorMessage = $"Edge references invalid node ID: SourceId '{sourceId}' or TargetId '{targetId}' not found.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }
                    }
                }

                // Validate subflows
                if (subflows != null)
                {
                    foreach (var subflow in subflows)
                    {
                        if (subflow == null) continue;
                        var name = subflow["Name"]?.ToString();
                        var subflowNodes = subflow["Nodes"]?.AsArray();
                        var subflowEdges = subflow["Edges"]?.AsArray();

                        if (string.IsNullOrEmpty(name) || subflowNodes == null || subflowEdges == null)
                        {
                            errorMessage = "Subflow missing required fields: Name, Nodes, or Edges.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        var subflowNodeIds = new HashSet<string>();
                        foreach (var node in subflowNodes)
                        {
                            if (node == null) continue;
                            var id = node["Id"]?.ToString();
                            var label = node["Label"]?.ToString();
                            var type = node["Type"]?.ToString();

                            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(type))
                            {
                                errorMessage = $"Subflow '{name}' node missing required fields: Id, Label, or Type.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!ValidNodeTypes.Contains(type))
                            {
                                errorMessage = $"Invalid NodeType '{type}' in subflow '{name}'. Valid types are: {string.Join(", ", ValidNodeTypes)}.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!subflowNodeIds.Add(id))
                            {
                                errorMessage = $"Duplicate node ID '{id}' in subflow '{name}'.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }
                        }

                        var subflowEdgeIds = new HashSet<string>();
                        foreach (var edge in subflowEdges)
                        {
                            if (edge == null) continue;
                            var id = edge["Id"]?.ToString();
                            var sourceId = edge["SourceId"]?.ToString();
                            var targetId = edge["TargetId"]?.ToString();
                            var type = edge["Type"]?.ToString();

                            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(type))
                            {
                                errorMessage = $"Subflow '{name}' edge missing required fields: Id, SourceId, TargetId, or Type.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!ValidEdgeTypes.Contains(type))
                            {
                                errorMessage = $"Invalid EdgeType '{type}' in subflow '{name}'. Valid types are: {string.Join(", ", ValidEdgeTypes)}.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!subflowEdgeIds.Add(id))
                            {
                                errorMessage = $"Duplicate edge ID '{id}' in subflow '{name}'.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!subflowNodeIds.Contains(sourceId) && !allNodeIds.Contains(sourceId))
                            {
                                errorMessage = $"Edge in subflow '{name}' references invalid SourceId '{sourceId}'.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }

                            if (!subflowNodeIds.Contains(targetId) && !allNodeIds.Contains(targetId))
                            {
                                errorMessage = $"Edge in subflow '{name}' references invalid TargetId '{targetId}'.";
                                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                                goto ContinueAttempt;
                            }
                        }
                    }
                }

                // Deserialize JSON to FlowchartDiagram
                var diagram = JsonSerializer.Deserialize<FlowchartDiagram>(
                    jsonResult,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    }
                );

                if (diagram == null)
                {
                    errorMessage = "Deserialization returned null diagram.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                logger.LogInformation("Attempt {attempt}: Generated diagram: {nodeCount} nodes, {edgeCount} edges, {subflowCount} subflows",
                    attempt, diagram.Nodes.Count, diagram.Edges?.Count ?? 0, diagram.Subflows?.Count ?? 0);

                return diagram;

                ContinueAttempt:
                continue;
            }
            catch (JsonException ex)
            {
                errorMessage = $"JSON parsing error: {ex.Message}. Please ensure the JSON is complete and valid.";
                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
            }
            catch (Exception ex) when (ex is not FormatException)
            {
                errorMessage = $"Unexpected error: {ex.Message}.";
                logger.LogError(ex, "Attempt {attempt}: Unexpected error", attempt);
            }
        }

        logger.LogError("Failed to generate a valid flowchart after {maxRetries} attempts.", MaxRetries);
        throw new FormatException($"Could not generate a valid flowchart after {MaxRetries} attempts.");
    }

    /// <summary>
    /// Generates a prompt for the LLM to extract flowchart elements from a use case specification.
    /// </summary>
    private string GetAnalysisPrompt(string useCaseSpec, string? errorMessage = null)
    {
        var prompt = $"""
            You are an expert Flowchart Analyzer that extracts and structures use case specifications into flowchart diagram components within the Text2Diagram_Backend.Features.Flowchart namespace.

            ### TASK:
            Analyze the following use case specification and generate a flowchart representation:
            {useCaseSpec}
            """
            +
            """
            ### INSTRUCTIONS:
            - Extract nodes, edges, and subflows based on the flow, adhering to the schema defined in Text2Diagram_Backend.Features.Flowchart.
            - Return only the structured JSON object in the following format, wrapped in code fences:
            ```json
            {
              "Nodes": [
                {"Id": "string", "Label": "string", "Type": "NodeType"}
              ],
              "Edges": [
                {"Id": "string", "SourceId": "string", "TargetId": "string", "Label": "string|null", "Type": "EdgeType"}
              ],
              "Subflows": [
                {"Name": "string", "Nodes": [{"Id": "string", "Label": "string", "Type": "NodeType"}], "Edges": [{"Id": "string", "SourceId": "string", "TargetId": "string", "Label": "string|null", "Type": "EdgeType"}]}
              ]
            }
            ```

            ### SCHEMA DETAILS:
            - FlowNode: {Id: string, Label: string, Type: NodeType}
            - FlowEdge: {Id: string, SourceId: string, TargetId: string, Label: string|null, Type: EdgeType}
            - Subflow: {Name: string, Nodes: List<FlowNode>, Edges: List<FlowEdge>}
            """ +
            $"""
            - NodeType: Enum with values [{string.Join(", ", ValidNodeTypes)}]
            - EdgeType: Enum with values [{string.Join(", ", ValidEdgeTypes)}]
            - Ensure:
              - All node IDs are unique within the main nodes and each subflow.
              - All edge IDs are unique within the main edges and each subflow.
              - Edge SourceId and TargetId reference valid node IDs (from main nodes or the respective subflow).
              - Use only the specified NodeType and EdgeType values.
              - The diagram has at least one node.
              - Nodes appear only in their appropriate context (main flow or subflow, not both).
              - Error subflows include a return path to the main flow where applicable (e.g., retry after error).
            - Do not include any text outside the JSON code fences.
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
            {
              "Nodes": [
                {"Id": "start_1", "Label": "User clicks 'Checkout' button", "Type": "Start"},
                {"Id": "process_1", "Label": "System asks for payment method", "Type": "Process"},
                {"Id": "process_2", "Label": "User enters credit card details", "Type": "Process"},
                {"Id": "decision_1", "Label": "Is payment valid?", "Type": "Decision"},
                {"Id": "process_3", "Label": "System displays order confirmation", "Type": "Process"},
                {"Id": "end_1", "Label": "Order completed", "Type": "End"}
              ],
              "Edges": [
                {"Id": "edge_1", "SourceId": "start_1", "TargetId": "process_1", "Label": "", "Type": "Normal"},
                {"Id": "edge_2", "SourceId": "process_1", "TargetId": "process_2", "Label": "", "Type": "Normal"},
                {"Id": "edge_3", "SourceId": "process_2", "TargetId": "decision_1", "Label": "", "Type": "Normal"},
                {"Id": "edge_4", "SourceId": "decision_1", "TargetId": "process_3", "Label": "Yes", "Type": "Success"},
                {"Id": "edge_5", "SourceId": "process_3", "TargetId": "end_1", "Label": "", "Type": "Normal"}
              ],
              "Subflows": [
                {
                  "Name": "invalid_payment",
                  "Nodes": [
                    {"Id": "error_1", "Label": "System shows error message", "Type": "Process"},
                    {"Id": "error_2", "Label": "System requests different payment method", "Type": "Process"}
                  ],
                  "Edges": [
                    {"Id": "error_edge_1", "SourceId": "decision_1", "TargetId": "error_1", "Label": "No", "Type": "Failure"},
                    {"Id": "error_edge_2", "SourceId": "error_1", "TargetId": "error_2", "Label": "", "Type": "Normal"},
                    {"Id": "error_edge_3", "SourceId": "error_2", "TargetId": "process_1", "Label": "", "Type": "Return"}
                  ]
                }
              ]
            }
            ```

            ### EDGE CASE EXAMPLE:
            INPUT:
            Use Case: Empty Flow
            Actor: User
            Basic Flow:
            1. User does nothing.
            
            OUTPUT:
            ```json
            {
              "Nodes": [
                {"Id": "start_1", "Label": "User does nothing", "Type": "Start"},
                {"Id": "end_1", "Label": "End", "Type": "End"}
              ],
              "Edges": [
                {"Id": "edge_1", "SourceId": "start_1", "TargetId": "end_1", "Label": "", "Type": "Normal"}
              ],
              "Subflows": []
            }
            ```
            """;

        if (errorMessage != null)
        {
            prompt += $"\n\n### PREVIOUS ERROR:\n{errorMessage}\nPlease correct the output to address this error and ensure the diagram meets all schema requirements.";
        }

        return prompt;
    }
}