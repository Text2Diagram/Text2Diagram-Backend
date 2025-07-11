using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class FlowchartDiagramEvaluator
{
    private readonly ILLMService2 _llmService2;
    private readonly ILogger<FlowchartDiagramEvaluator> _logger;

    public FlowchartDiagramEvaluator(ILLMService2 llmService2, ILogger<FlowchartDiagramEvaluator> logger)
    {
        _llmService2 = llmService2;
        _logger = logger;
    }

    public async Task<FlowchartDiagram> EvaluateFlowchartDiagramAsync(string useCaseSpec, FlowchartDiagram flowchart)
    {
        var diagramJson = JsonSerializer.Serialize(flowchart, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Evaluating flowchart diagram: {DiagramJson}", diagramJson);


        var prompt = """
            You are an expert software architecture AI specializing in validating and improving flowchart diagrams against user requirements. Your task is to:

            1. Analyze the correctness, completeness, and logical consistency of a flowchart diagram based on a provided use case specification.
            2. Identify any errors, missing elements, or inconsistencies in the flowchart diagram.
            3. Suggest specific corrections or improvements to align the flowchart with the use case specification.

            Input Details:
            - Use Case Specification: A textual description of the process or system requirements.
            - Flowchart Diagram: A JSON object with the following structure:
              ```json
              {
                "Flows": [
                  {
                    "Name": string,
                    "FlowType": string,
                    "Nodes": [
                      {
                        "Id": "",
                        "Label": "",
                        "Type": ""
                      }
                    ],
                    "Edges": [
                      {
                        "SourceId": "",
                        "TargetId": "",
                        "Type": "",
                        "Label": ""
                      }
                    ]
                  }
                ],
                ""BranchingPoints"": [
                  {
                    ""SubFlowName"": "",
                    ""BranchNodeId"": ""
                  }
                ]
              }
              ```
           """
           +
           $"""
            Node Rules: {Prompts.NodeRules}
            Edge Rules: {Prompts.EdgeRules}
            
            Evaluate the flowchart diagram against the use case specification provided below:
            {useCaseSpec}
            The original flowchart diagram is as follows:
            {diagramJson}

            Return the corrected flowchart diagram in the same JSON format. 
            Only correct the flowchart if it does not accurately reflect the use case specification or if there are identified issues.
            Else return the original flowchart without changes.
            Do not include any additional text or explanations in the response.
            Ensure that the returned diagram accurately reflects the use case specification and addresses any identified issues.
            {Prompts.LanguageRules}
            """;

        var response = await _llmService2.GenerateContentAsync(prompt);
        var json = FlowchartHelpers.ValidateJson(response.Content);
        _logger.LogInformation("Evaluating flowchart diagram: {Response}", json);
        var correctedFlowchart = JsonSerializer.Deserialize<FlowchartDiagram>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return correctedFlowchart ?? throw new InvalidOperationException("Failed to parse the corrected flowchart diagram from the response.");
    }
}
