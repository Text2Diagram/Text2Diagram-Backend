using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Agents;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Features.Helper;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;
using Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc;

namespace Text2Diagram_Backend.Features.ERD;

/// <summary>
/// Analyzes domain descriptions to extract elements for Entity Relationship Diagram (ERD) generation.
/// This analyzer is optimized for text describing entities and relationships.
/// </summary>
public class AnalyzerForER
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForER> logger;
    private readonly ILLMService _llmService;
	private const int MaxRetries = 1;
    private static readonly string[] ValidRelationshipTypes = Enum.GetNames(typeof(RelationshipType));
    private static readonly string[] ValidPropertyRoles = new[] { "PK", "FK", "" };

    public AnalyzerForER(Kernel kernel, ILogger<AnalyzerForER> logger, ILLMService lLMService)
    {
        this.kernel = kernel;
        this.logger = logger;
		_llmService = lLMService;
	}

    /// <summary>
    /// Analyzes a domain description to generate an Entity Relationship Diagram.
    /// </summary>
    /// <param name="domainDescription">The domain description text to analyze.</param>
    /// <returns>An ER diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the domain description is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<ERDiagram> AnalyzeAsync(string domainDescription)
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
                var responseIdentifyEntities = await _llmService.GenerateContentAsync(promtIdentifyEntities);
                var textContent = responseIdentifyEntities.Content ?? "";
                var finalIdentifyEntities = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
                var listEntity = DeserializeLLMResponseFunc.DeserializeLLMResponse<string>(finalIdentifyEntities);
                //step 2: Identify properties
                string promtIdentifyProperties = Step2_IdentifyProperty.PromtIdentifyProperty(domainDescription, JsonConvert.SerializeObject(listEntity));
                var responseIdentifyProperties = await _llmService.GenerateContentAsync(promtIdentifyProperties);
                textContent = responseIdentifyProperties.Content ?? "";
                var finalIdentifyProperties = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
                var listCompletedEntity = DeserializeLLMResponseFunc.DeserializeLLMResponse<Entity>(finalIdentifyProperties);
                //step 3: Identify relationships
                string promtIdentifyRelationships = Step3_IdentifyRelation.PromtIdentifyRelation(domainDescription, JsonConvert.SerializeObject(listEntity));
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
				return diagram;
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

    /// <summary>
    /// Generates a prompt for the LLM to extract ER diagram elements from a domain description.
    /// </summary>
    private string GetAnalysisPrompt(string domainDescription, string? errorMessage = null)
    {
        var prompt = $"""
            You are an expert Entity Relationship Analyzer tasked with extracting and structuring a domain description into Entity Relationship Diagram (ERD) components within the Text2Diagram_Backend.Features.ERD namespace.

            ### TASK:
            Analyze the following domain description and structure it into a complete Entity Relationship Diagram representation:
            {domainDescription}

            ### INSTRUCTIONS:
            - Identify entities (e.g., Student, Course) and their properties from the domain description.
            - Infer logical properties if not provided (e.g., id as PK, name).
            - Define relationships based on entity interactions (e.g., "Student enrolls in Course").
            - Return only the structured JSON object in the following format, wrapped in code fences:
           """
            +
           """
            ```json
            {
              "Entites": [
                {
                  "Name": "ENTITY_NAME",
                  "Properties": [
                    {"Type": "string", "Name": "propName", "Role": "PK", "Description": "prop purpose"}
                  ]
                }
              ],
              "Relationships": [
                {
                  "SourceEntityName": "ENTITY1",
                  "DestinationEntityName": "ENTITY2",
                  "SourceRelationshipType": "RelationshipType",
                  "DestinationRelationshipType": "RelationshipType",
                  "Description": "relationship purpose"
                }
              ]
            }
            ```
            
            ### SCHEMA DETAILS:
            - Entity: {{Name: string, Properties: List<Property>}}
            - Property: {{Type: string, Name: string, Role: string, Description: string}}
            - Relationship: {{SourceEntityName: string, DestinationEntityName: string, SourceRelationshipType: RelationshipType, DestinationRelationshipType: RelationshipType, Description: string}}
            """
            +
            $"""
            - RelationshipType: Enum with values [{string.Join(", ", ValidRelationshipTypes)}]
            - Valid Property Roles: ["PK", "FK", ""]
            - Ensure:
              - Entity names are uppercase, unique, and contain no spaces or special characters (e.g., STUDENT).
              - Property names contain no spaces (e.g., studentId).
              - Property Type is a valid data type (e.g., string, int, float, string[]).
              - Role is one of: PK (primary key), FK (foreign key), or empty string ("").
              - SourceEntityName and DestinationEntityName reference valid entity names.
              - Use only the specified RelationshipType values.
              - The diagram has at least one entity.
              - All fields are required and must not be null or empty (except Role, which can be an empty string).
              - Do not include any text outside the JSON code fences.
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            A system to manage students and courses they enroll in.
            
            OUTPUT:
            ```json
            {
              "Entites": [
                {
                  "Name": "STUDENT",
                  "Properties": [
                    {"Type": "string", "Name": "id", "Role": "PK", "Description": "student identifier"},
                    {"Type": "string", "Name": "name", "Role": "", "Description": "student name"}
                  ]
                },
                {
                  "Name": "COURSE",
                  "Properties": [
                    {"Type": "string", "Name": "id", "Role": "PK", "Description": "course identifier"},
                    {"Type": "string", "Name": "title", "Role": "", "Description": "course title"}
                  ]
                }
              ],
              "Relationships": [
                {
                  "SourceEntityName": "STUDENT",
                  "DestinationEntityName": "COURSE",
                  "SourceRelationshipType": "ZeroOrMore",
                  "DestinationRelationshipType": "ZeroOrMore",
                  "Description": "enrolls in"
                }
              ]
            }
            ```

            ### EDGE CASE EXAMPLE:
            INPUT:
            A system with only departments.
            
            OUTPUT:
            ```json
            {
              "Entites": [
                {
                  "Name": "DEPARTMENT",
                  "Properties": [
                    {"Type": "string", "Name": "id", "Role": "PK", "Description": "department identifier"},
                    {"Type": "string", "Name": "name", "Role": "", "Description": "department name"}
                  ]
                }
              ],
              "Relationships": []
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