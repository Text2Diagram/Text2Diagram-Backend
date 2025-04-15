using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text.Json;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Flowchart;

/// <summary>
/// Analyzes structured use case specifications to extract elements for flowchart generation.
/// This analyzer is optimized for structured text following use case specification format.
/// </summary>
public class UseCaseSpecAnalyzerForFlowchart : IAnalyzer<FlowchartDiagram>
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<UseCaseSpecAnalyzerForFlowchart> logger;

    public UseCaseSpecAnalyzerForFlowchart(
        OllamaProvider provider,
        IConfiguration configuration,
        ILogger<UseCaseSpecAnalyzerForFlowchart> logger)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
    }

    /// <summary>
    /// Analyzes a structured use case specification to extract and generate a flowchart diagram directly.
    /// </summary>
    /// <param name="useCaseSpec">The use case specification text to analyze.</param>
    /// <returns>A flowchart diagram ready for rendering.</returns>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<FlowchartDiagram> AnalyzeAsync(string useCaseSpec)
    {
        try
        {
            var prompt = GetAnalysisPrompt(useCaseSpec);
            var response = await llm.GenerateAsync(prompt);

            logger.LogInformation("LLM response: {response}", response);

            var diagram = ParseAndValidateResponse(response);
            if (diagram == null)
            {
                logger.LogError("Failed to extract valid flowchart diagram from LLM response");
                throw new FormatException("Error while analyzing use case specification.");
            }

            return diagram;
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            logger.LogError(ex, "Unexpected error during use case specification analysis");
            throw new FormatException("Failed to analyze use case specification due to an internal error.", ex);
        }
    }

    /// <summary>
    /// Generates a prompt for the LLM to extract flowchart elements from a use case specification.
    /// </summary>
    private string GetAnalysisPrompt(string useCaseSpec)
    {
        return $"""
            You are an expert Flowchart Analyzer that extracts and structures use case specifications into diagram-ready components.

            ### TASK: Extract, organize, and structure the following use case specification into a complete flowchart representation:
            {useCaseSpec}

            """ +
            """
            ### EXTRACTION AND STRUCTURING RULES:

            1. NODE IDENTIFICATION:
               - Extract START nodes (triggers like "User clicks Submit", "System receives notification")
               - Extract PROCESS nodes (steps in the main, alternative, and exception flows)
               - Extract DECISION nodes (all conditional checks, validation points that branch flow)
               - Extract END nodes (final outcomes, results, or completion points)
               - Ensure each node has a unique identifiable label

            2. CONNECTION MAPPING:
               - Map the SEQUENTIAL flow between nodes in the main path
               - Map BRANCH connections from decision nodes to alternative paths
               - Map ERROR connections from validation points to exception paths
               - Identify RETURN points where flows merge back into the main path
               - Label connections with conditions where applicable

            3. FLOW ORGANIZATION:
               - MainFlow: Core success path from trigger to completion
               - AlternativeFlows: Named variations with clear entry/exit points
               - ExceptionFlows: Error paths with clear triggering conditions

            4. DIAGRAM OPTIMIZATION:
               - Direction: Determine if the flow is better represented vertically (TD) or horizontally (LR)
               - Subflows: Group related steps into logical subflows
               - Decision Structure: Format decisions as yes/no questions ending with "?"

            ### VALID NODE TYPES:
            You MUST use ONLY these exact NodeType enum values:
            - Start: Beginning of process flow
            - End: End of process flow
            - Process: Standard process step
            - Decision: Decision point
            - Input: Data input
            - Output: Data output
            - Display: Display information
            - Document: Single document
            - MultiDocument: Multiple documents
            - File: File
            - Preparation: Preparation step
            - ManualInput: Manual input
            - ManualOperation: Manual operation
            - PredefinedProcess: Predefined process
            - UserDefinedProcess: User-defined process
            - DividedProcess: Process with divisions
            - Database: Database
            - DirectAccessStorage: Direct access storage
            - DiskStorage: Disk storage
            - StoredData: Stored data/tape
            - ExternalStorage: External storage
            - Internal: Internal storage
            - Connector: Connection point
            - OffPageConnector: Off-page connector
            - Delay: Delay/wait
            - Loop: Loop
            - LoopLimit: Loop limit
            - Merge: Merge paths
            - Or: OR junction
            - SummingJunction: Summing junction
            - Sort: Sort junction
            - Collate: Collate
            - Card: Information card
            - Comment: Left-side comment
            - CommentRight: Right-side comment
            - Comments: Two-sided comment
            - ComLink: Communication link

            ### VALID EDGE TYPES:
            You MUST use ONLY these exact EdgeType enum values:
            - Normal: Regular arrow
            - Thick: Thick line
            - Dotted: Dotted line
            - Success: Success path
            - Failure: Failure path
            - Conditional: Conditional path
            - Return: Return path
            - NoArrow: Line without arrow
            - OpenLink: Link with open arrow
            - CrossLink: Link with a cross
            - CircleEnd: Circle on the end
            - CrossEnd: Cross on the end
            - DottedNoArrow: Dotted line without arrow
            - DottedOpenLink: Dotted line with open arrow
            - DottedCrossLink: Dotted line with cross
            - ThickNoArrow: Thick line without arrow
            - ThickOpenLink: Thick line with open arrow
            - ThickCrossLink: Thick line with cross

            OUTPUT JSON STRUCTURE (all fields required):
            {
              "Nodes": [
                {"Id": "start_1", "Label": "User clicks 'Checkout'", "Type": "Start"},
                {"Id": "process_1", "Label": "System displays payment options", "Type": "Process"},
                {"Id": "decision_1", "Label": "Is payment method valid?", "Type": "Decision"},
                {"Id": "end_1", "Label": "Order completed", "Type": "End"}
              ],
              "Edges": [
                {"Id": "edge_1", "SourceId": "start_1", "TargetId": "process_1", "Label": "", "Type": "Normal"},
                {"Id": "edge_2", "SourceId": "process_1", "TargetId": "decision_1", "Label": "", "Type": "Normal"},
                {"Id": "edge_3", "SourceId": "decision_1", "TargetId": "end_1", "Label": "Yes", "Type": "Success"}
              ],
              "Subflows": [
                {
                  "Name": "payment_failure",
                  "Nodes": [
                    {"Id": "error_1", "Label": "Display error message", "Type": "Process"}
                  ],
                  "Edges": [
                    {"Id": "error_edge_1", "SourceId": "decision_1", "TargetId": "error_1", "Label": "No", "Type": "Failure"}
                  ]
                }
              ]
            }

            EXAMPLE CONVERSION:
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

            IMPORTANT:
            - Generate VALID JSON only (proper quotes, commas, brackets)
            - Ensure all IDs are unique
            - Ensure edge sources and targets reference existing node IDs
            - Do NOT include any explanations, comments, or text outside the JSON structure
            - ONLY use the exact NodeType and EdgeType enum values provided above

            Focus on extracting the complete diagram structure, not just the elements.
            """;
    }

    /// <summary>
    /// Parses and validates the LLM response to extract the flowchart diagram.
    /// </summary>
    /// <param name="response">The raw response from the LLM.</param>
    /// <returns>The extracted flowchart diagram, or null if extraction failed.</returns>
    private FlowchartDiagram? ParseAndValidateResponse(string response)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                logger.LogError("Empty response received from LLM");
                return null;
            }

            string pattern = @"
                        \{
                        (?>            
                            [^{}]+     
                          | (?<open>\{)  
                          | (?<-open>\}) 
                        )*              
                        (?(open)(?!))  
                        \}
                    ";

            Match match = Regex.Match(
                response,
                pattern,
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline
            );

            if (!match.Success)
            {
                logger.LogError("Failed to find valid JSON in LLM response");
                return null;
            }

            var json = match.Groups[1].Value.Trim();

            if (string.IsNullOrWhiteSpace(json))
            {
                json = match.Groups[0].Value.Trim();
            }

            logger.LogInformation("Extracted JSON: {json}", json);

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            try
            {
                var result = JsonSerializer.Deserialize<FlowchartDiagram>(json, serializerOptions);

                if (result == null)
                {
                    logger.LogError("Deserialization returned null");
                    return null;
                }

                if (result.Nodes == null || !result.Nodes.Any())
                {
                    logger.LogError("No nodes found in the extracted diagram");
                    return null;
                }

                if (result.Edges == null || !result.Edges.Any())
                {
                    logger.LogWarning("No edges found in the extracted diagram");
                    result = result with { Edges = new List<FlowEdge>() };
                }

                if (result.Subflows == null)
                {
                    logger.LogWarning("No subflows found in the extracted diagram");
                    result = result with { Subflows = new List<Subflow>() };
                }


                // Validate edge connections
                var nodeIds = new HashSet<string>(result.Nodes.Select(n => n.Id));

                // Add subflow node IDs
                if (result.Subflows != null)
                {
                    foreach (var subflow in result.Subflows)
                    {
                        if (subflow.Nodes != null)
                        {
                            foreach (var node in subflow.Nodes)
                            {
                                nodeIds.Add(node.Id);
                            }
                        }
                    }
                }

                var validEdges = result.Edges
                    .Where(edge => nodeIds.Contains(edge.SourceId) && nodeIds.Contains(edge.TargetId))
                    .ToList();

                var invalidEdges = result.Edges
                    .Where(edge => !nodeIds.Contains(edge.SourceId) || !nodeIds.Contains(edge.TargetId))
                    .ToList();

                if (invalidEdges.Any())
                {
                    foreach (var edge in invalidEdges)
                    {
                        logger.LogWarning("Invalid edge connection: Edge '{edgeId}' - Source '{source}' or Target '{target}' node not found",
                            edge.Id, edge.SourceId, edge.TargetId);
                    }

                    logger.LogWarning("Removed {count} edges with invalid source/target references", invalidEdges.Count);
                    result = result with { Edges = validEdges };
                }

                // Validate edges within subflows
                if (result.Subflows != null && result.Subflows.Any())
                {
                    var updatedSubflows = new List<Subflow>();
                    
                    foreach (var subflow in result.Subflows)
                    {
                        // Create a set of valid node IDs for this subflow
                        var subflowNodeIds = new HashSet<string>();
                        
                        // Add all nodes from the main diagram
                        foreach (var nodeId in nodeIds)
                        {
                            subflowNodeIds.Add(nodeId);
                        }
                        
                        // Add nodes specific to this subflow
                        if (subflow.Nodes != null)
                        {
                            foreach (var node in subflow.Nodes)
                            {
                                subflowNodeIds.Add(node.Id);
                            }
                        }
                        
                        // Filter out invalid edges
                        var validSubflowEdges = subflow.Edges
                            .Where(edge => subflowNodeIds.Contains(edge.SourceId) && subflowNodeIds.Contains(edge.TargetId))
                            .ToList();
                        
                        var invalidSubflowEdges = subflow.Edges
                            .Where(edge => !subflowNodeIds.Contains(edge.SourceId) || !subflowNodeIds.Contains(edge.TargetId))
                            .ToList();
                        
                        if (invalidSubflowEdges.Any())
                        {
                            foreach (var edge in invalidSubflowEdges)
                            {
                                logger.LogWarning("Invalid edge in subflow '{subflowName}': Edge '{edgeId}' - Source '{source}' or Target '{target}' node not found",
                                    subflow.Name, edge.Id, edge.SourceId, edge.TargetId);
                            }
                            
                            logger.LogWarning("Removed {count} edges with invalid source/target references from subflow '{subflowName}'", 
                                invalidSubflowEdges.Count, subflow.Name);
                        }
                        
                        // Create updated subflow with valid edges
                        var updatedSubflow = subflow with { Edges = validSubflowEdges };
                        updatedSubflows.Add(updatedSubflow);
                    }
                    
                    // Update the result with the corrected subflows
                    result = result with { Subflows = updatedSubflows };
                }

                // Log structure stats after processing
                logger.LogInformation(
                    "Final diagram structure: {nodeCount} nodes ({startCount} start, {decisionCount} decision, {endCount} end), {edgeCount} edges, {subflowCount} subflows",
                    result.Nodes.Count,
                    result.Nodes.Count(n => n.Type == NodeType.Start),
                    result.Nodes.Count(n => n.Type == NodeType.Decision),
                    result.Nodes.Count(n => n.Type == NodeType.End),
                    result.Edges.Count,
                    result.Subflows?.Count ?? 0);

                return result;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON deserialization error: {message}", ex.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing LLM response: {message}", ex.Message);
            return null;
        }
    }
}
