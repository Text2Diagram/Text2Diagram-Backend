using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Features.ERD.Agents;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Features.Helper;
using Text2Diagram_Backend.Features.Sequence.Agent.Objects;
using Text2Diagram_Backend.Features.Sequence.Agent;
using Text2Diagram_Backend.Features.Sequence.NewWay;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;
using Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc;
using Text2Diagram_Backend.Middlewares;

namespace Text2Diagram_Backend.Features.ERD;

/// <summary>
/// Analyzes domain descriptions to extract elements for Entity Relationship Diagram (ERD) generation.
/// This analyzer is optimized for text describing entities and relationships.
/// </summary>
public class AnalyzerForER
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForER> logger;
    private readonly ILLMService1 _llmService;
	private const int MaxRetries = 1;
    private static readonly string[] ValidRelationshipTypes = Enum.GetNames(typeof(RelationshipType));
    private static readonly string[] ValidPropertyRoles = new[] { "PK", "FK", "" };
	private readonly IHubContext<ThoughtProcessHub> _hubContext;

	public AnalyzerForER(Kernel kernel, ILogger<AnalyzerForER> logger, ILLMService1 lLMService, IHubContext<ThoughtProcessHub> hubContext)
    {
        this.kernel = kernel;
        this.logger = logger;
		_llmService = lLMService;
		_hubContext = hubContext;
	}

    /// <summary>
    /// Analyzes a domain description to generate an Entity Relationship Diagram.
    /// </summary>
    /// <param name="domainDescription">The domain description text to analyze.</param>
    /// <returns>An ER diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the domain description is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<DiagramContent> AnalyzeAsync(string domainDescription)
    {
        if (string.IsNullOrWhiteSpace(domainDescription))
        {
            logger.LogError("Domain description is empty or null.");
            throw new ArgumentException("Domain description cannot be empty.", nameof(domainDescription));
        }

        string? errorMessage = null;
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                //step 1: Identify entities
                string promtIdentifyEntities = Step1_IdentifyEntity.GetPromtIdentifyEntity(domainDescription);
				await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Identifying entities....");
                var responseIdentifyEntities = await _llmService.GenerateContentAsync(promtIdentifyEntities);
                var textContent = responseIdentifyEntities.Content ?? "";
				var finalIdentifyEntities = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
                var listEntity = DeserializeLLMResponseFunc.DeserializeLLMResponse<string>(finalIdentifyEntities);
                //step 2: Identify properties
                string promtIdentifyProperties = Step2_IdentifyProperty.PromtIdentifyProperty(domainDescription, JsonConvert.SerializeObject(listEntity));
				await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Identifying properties...");
                var responseIdentifyProperties = await _llmService.GenerateContentAsync(promtIdentifyProperties);
                textContent = responseIdentifyProperties.Content ?? "";
				var finalIdentifyProperties = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
                var listCompletedEntity = DeserializeLLMResponseFunc.DeserializeLLMResponse<Entity>(finalIdentifyProperties);
                //step 3: Identify relationships
                string promtIdentifyRelationships = Step3_IdentifyRelation.PromtIdentifyRelation(domainDescription, JsonConvert.SerializeObject(listEntity));
				await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Identifying relationships...");
                var responseIdentifyRelationships = await _llmService.GenerateContentAsync(promtIdentifyRelationships);
                textContent = responseIdentifyRelationships.Content ?? "";
				var finalIdentifyRelationships = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
                var listRelationship = DeserializeLLMResponseFunc.DeserializeLLMResponse<Relationship>(finalIdentifyRelationships);
				// Deserialize JSON to ERDiagram
				var diagram = new ERDiagram
                {
                    Entites = listCompletedEntity,
					Relationships = listRelationship
				};
                await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generating ER diagram...");
                string mermaidCode = GenerateMermaidCode(diagram);
				//step 4: Validate and structure the ER diagram
                string promtEvaluateERDiagram = EvaluateERDiagram.PromptEvaluateERDiagram(domainDescription, mermaidCode);
				await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Evaluating ER diagram...");
				var responseEvaluate = await _llmService.GenerateContentAsync(promtEvaluateERDiagram);
				var textEvaluate = responseEvaluate.Content ?? "";
				var finalEvaluate = ExtractJsonFromTextHelper.ExtractJsonFromText(textEvaluate);
				var evaluateResult = DeserializeLLMResponseFunc.DeserializeLLMResponse<EvaluateResponseDto>(finalEvaluate);
				// Check if the evaluation indicates the diagram is accurate
				string diagramJson = JsonConvert.SerializeObject(diagram);
				if (evaluateResult == null || evaluateResult.Count == 0 || evaluateResult[0].IsAccurate)
				{
                    await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated ER diagram successfully!");
                    // If the diagram is accurate, return it
                    return new DiagramContent()
					{
						mermaidCode = mermaidCode,
						diagramJson = diagramJson
					};
				}
				else
				{
					var promtValidate = PromtForRegenER.GetPromtForRegenER(JsonConvert.SerializeObject(evaluateResult[0]), diagramJson);
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Modifying ER diagram...");
					var responseValidate = await _llmService.GenerateContentAsync(promtValidate);
					var textValidate = responseValidate.Content ?? "";
					var final = ExtractJsonFromTextHelper.ExtractJsonFromText(textValidate);
					// Deserialize JSON to ERDiagram
					var target = DeserializeLLMResponseFunc.DeserializeLLMResponse<ERDiagram>(final);
					string mermaidCodeAfterEvaluate = GenerateMermaidCode(target[0]);
                    await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated ER diagram successfully!");
                    return new DiagramContent()
					{
						mermaidCode = mermaidCodeAfterEvaluate,
						diagramJson = JsonConvert.SerializeObject(target[0])
					};
				}
            }
            catch (System.Text.Json.JsonException ex)
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

        logger.LogError("Failed to generate a valid ER diagram after {maxRetries} attempts.", MaxRetries);
        throw new FormatException($"Could not generate a valid ER diagram after {MaxRetries} attempts.");
    }


    public async Task<DiagramContent> AnalyzeForReGenAsync(string feedback, string diagramJson)
    {
		string promtRegenForEr = PromtForRegenER.GetPromtForRegenER(feedback, diagramJson);
		var res = await _llmService.GenerateContentAsync(promtRegenForEr);
		string textContent = res.Content ?? "";
		var final = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
        // Deserialize JSON to ERDiagram
        var target = DeserializeLLMResponseFunc.DeserializeLLMResponse<ERDiagram>(final);
        string mermaidCode = GenerateMermaidCode(target[0]);
		return new DiagramContent()
		{
			mermaidCode = mermaidCode,
			diagramJson = JsonConvert.SerializeObject(target[0])
		};
	}
	/// <summary>
	/// Generates a prompt for the LLM to extract ER diagram elements from a domain description.
	/// </summary>
	private string GenerateMermaidCode(ERDiagram diagram)
	{
		var mermaid = new StringBuilder();

		// Start ER diagram definition
		mermaid.AppendLine("erDiagram");

		foreach (var entity in diagram.Entites)
		{
			mermaid.AppendLine($"    {entity.Name} {{");
			foreach (var prop in entity.Properties)
			{
				mermaid.AppendLine($"        {prop.Type} {prop.Name.Trim().Replace(' ', '_')} {prop.Role} \"{prop.Description}\"");
			}
			mermaid.AppendLine("    }");
		}

		foreach (var relation in diagram.Relationships)
		{
			string sourceConnector = GetEdgeConnector(relation.DestinationRelationshipType, true);
			string destConnector = GetEdgeConnector(relation.SourceRelationshipType, false);
			mermaid.AppendLine($"    {relation.SourceEntityName} {sourceConnector}--{destConnector} {relation.DestinationEntityName} : \"{relation.Description}\"");
		}

		return mermaid.ToString();
	}

	/// <summary>
	/// Determines the appropriate connector symbol for a relationship type.
	/// </summary>
	/// <param name="type">The relationship type to evaluate</param>
	/// <param name="isLeft">Indicates if this is the left (source) side of the relationship</param>
	/// <returns>A string representing the Mermaid.js connector symbol</returns>
	private string GetEdgeConnector(RelationshipType type, bool isLeft)
	{
		return type switch
		{
			RelationshipType.ZeroOrOne => isLeft ? "|o" : "o|",
			RelationshipType.ExactlyOne => "||",
			RelationshipType.ZeroOrMore => isLeft ? "}o" : "o{",
			RelationshipType.OneOrMore => isLeft ? "}|" : "|{",
			_ => isLeft ? "|o" : "o|"
		};
	}
}