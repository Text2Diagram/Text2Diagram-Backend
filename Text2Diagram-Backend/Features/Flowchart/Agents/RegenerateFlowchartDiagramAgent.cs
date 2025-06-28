using System.Text.Json;
using System.Text.Json.Nodes;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart.Agents
{
    public class RegenerateFlowchartDiagramAgent
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<RegenerateFlowchartDiagramAgent> _logger;
        private readonly FlowchartDiagramGenerator _flowchartDiagramGenerator;

        public RegenerateFlowchartDiagramAgent(
            ILLMService llmService,
            ILogger<RegenerateFlowchartDiagramAgent> logger,
            FlowchartDiagramGenerator flowchartDiagramGenerator)
        {
            _llmService = llmService;
            _logger = logger;
            _flowchartDiagramGenerator = flowchartDiagramGenerator;
        }

        public async Task<string> RegenerateAsync(string feedback, string diagramDataJson)
        {
            _logger.LogInformation("Regenerating flowchart with feedback: {Feedback}", feedback);

            var response = await ApplyCommandsAsync(feedback, diagramDataJson);
            var jsonNode = FlowchartHelpers.ValidateJson(response);

            var flowsNode = jsonNode["Flows"];
            var branchingPointsNode = jsonNode["BranchingPoints"];

            if (flowsNode == null || branchingPointsNode == null)
            {
                _logger.LogError("Invalid JSON structure: 'Flows' or 'BranchingPoints' missing");
                throw new InvalidOperationException("Invalid JSON structure: 'Flows' or 'BranchingPoints' missing");
            }

            var flows = flowsNode.AsArray()
                .Select(node => node.Deserialize<Flow>())
                .Where(flow => flow != null)
                .Cast<Flow>()
                .ToList();

            var branchingPoints = branchingPointsNode.AsArray()
                .Select(node => node.Deserialize<BranchingPoint>())
                .Where(bp => bp != null)
                .Cast<BranchingPoint>()
                .ToList();

            if (!flows.Any())
            {
                _logger.LogWarning("No valid flows parsed from JSON");
                throw new InvalidOperationException("No valid flows parsed from JSON");
            }

            var flowchart = new FlowchartDiagram(flows, branchingPoints);
            var mermaidCode = await _flowchartDiagramGenerator.GenerateMermaidCodeAsync(flowchart);
            return mermaidCode;
        }

        private async Task<string> ApplyCommandsAsync(string feedback, string diagramDataJson)
        {
            _logger.LogInformation("Applying feedback to flowchart JSON using LLM service");

            // Parse the input JSON to ensure it's valid
            JsonNode? jsonNode;
            try
            {
                jsonNode = JsonNode.Parse(diagramDataJson);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to parse diagramDataJson: {Error}", ex.Message);
                throw new InvalidOperationException("Invalid diagram JSON format", ex);
            }

            if (jsonNode == null)
            {
                _logger.LogError("Parsed JSON is null");
                throw new InvalidOperationException("Parsed JSON is null");
            }

            var prompt = $"""
                You are a flowchart diagram modification assistant. 
                Modify the provided flowchart JSON based on the given feedback. 
                Ensure the output is a valid JSON string wrapped in ```json\n...\n``` code fences, 
                maintaining the structure with 'Flows' and 'BranchingPoints'. 
                The diagram is a flowchart, and the JSON includes flows with nodes (Id, Type, Label) 
                and edges (SourceId, TargetId, Type, Label), and branching points (SubFlowName, BranchNodeId). 
                Node Rules: {Prompts.NodeRules}
                Edge Rules: {Prompts.EdgeRules}
                Current JSON: {diagramDataJson}
                Feedback: {feedback}
                Instruction: Interpret the feedback and modify the flowchart JSON accordingly. 
                For example, if the feedback requests adding a node, add it to the appropriate flow's 'Nodes' array and update the 'Edges' array to maintain connectivity. 
                Ensure unique node IDs, valid node/edge types, and a consistent flowchart structure.
                """;
            try
            {
                var llmResponse = await _llmService.GenerateContentAsync(prompt);
                if (string.IsNullOrEmpty(llmResponse?.Content))
                {
                    _logger.LogWarning("LLM returned empty response for feedback: {Feedback}", feedback);
                    throw new InvalidOperationException("LLM returned empty response");
                }

                // Validate and extract the JSON from the LLM response
                var updatedJsonNode = FlowchartHelpers.ValidateJson(llmResponse.Content);
                if (updatedJsonNode == null)
                {
                    _logger.LogError("LLM response does not contain valid JSON for feedback: {Feedback}", feedback);
                    throw new InvalidOperationException("LLM response does not contain valid JSON");
                }

                _logger.LogInformation("Applied feedback to flowchart JSON");
                return llmResponse.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying feedback: {Error}", ex.Message);
                throw;
            }
        }
    }
}