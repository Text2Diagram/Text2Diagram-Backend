using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;

namespace Text2Diagram_Backend.Features.ERD;

/// <summary>
/// Analyzes domain descriptions to extract elements for Entity Relationship Diagram (ERD) generation.
/// This analyzer is optimized for text describing entities and relationships.
/// </summary>
public class AnalyzerForER
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForER> logger;
    private const int MaxRetries = 3;
    private static readonly string[] ValidRelationshipTypes = Enum.GetNames(typeof(RelationshipType));
    private static readonly string[] ValidPropertyRoles = new[] { "PK", "FK", "" };

    public AnalyzerForER(Kernel kernel, ILogger<AnalyzerForER> logger)
    {
        this.kernel = kernel;
        this.logger = logger;
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
                var prompt = GetAnalysisPrompt(domainDescription, errorMessage);
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

                var entities = jsonNode["Entites"]?.AsArray();
                var relationships = jsonNode["Relationships"]?.AsArray();

                if (entities == null || entities.Count == 0)
                {
                    errorMessage = "Entites array is missing or empty.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                // Validate entities
                var entityNames = new HashSet<string>();
                foreach (var entity in entities)
                {
                    if (entity == null) continue;
                    var name = entity["Name"]?.ToString();
                    var properties = entity["Properties"]?.AsArray();

                    if (string.IsNullOrEmpty(name) || properties == null)
                    {
                        errorMessage = "Entity missing required fields: Name or Properties.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    if (!name.All(c => char.IsUpper(c) || char.IsDigit(c)))
                    {
                        errorMessage = $"Entity name '{name}' must be uppercase with no spaces or special characters.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    if (!entityNames.Add(name))
                    {
                        errorMessage = $"Duplicate entity name '{name}' found.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    if (properties.Count == 0)
                    {
                        errorMessage = $"Entity '{name}' has no properties.";
                        logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                        goto ContinueAttempt;
                    }

                    var propertyNames = new HashSet<string>();
                    foreach (var prop in properties)
                    {
                        if (prop == null) continue;
                        var propType = prop["Type"]?.ToString();
                        var propName = prop["Name"]?.ToString();
                        var role = prop["Role"]?.ToString();
                        var description = prop["Description"]?.ToString();

                        if (string.IsNullOrEmpty(propType) || string.IsNullOrEmpty(propName) || role == null || string.IsNullOrEmpty(description))
                        {
                            errorMessage = $"Property in entity '{name}' missing required fields: Type, Name, Role, or Description.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!ValidPropertyRoles.Contains(role))
                        {
                            errorMessage = $"Invalid Role '{role}' in property '{propName}' of entity '{name}'. Valid roles are: {string.Join(", ", ValidPropertyRoles)}.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (propName.Contains(" "))
                        {
                            errorMessage = $"Property name '{propName}' in entity '{name}' contains spaces.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!propertyNames.Add(propName))
                        {
                            errorMessage = $"Duplicate property name '{propName}' in entity '{name}'.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }
                    }
                }

                // Validate relationships
                if (relationships != null)
                {
                    foreach (var rel in relationships)
                    {
                        if (rel == null) continue;
                        var sourceEntityName = rel["SourceEntityName"]?.ToString();
                        var destinationEntityName = rel["DestinationEntityName"]?.ToString();
                        var sourceRelType = rel["SourceRelationshipType"]?.ToString();
                        var destRelType = rel["DestinationRelationshipType"]?.ToString();
                        var description = rel["Description"]?.ToString();

                        if (string.IsNullOrEmpty(sourceEntityName) || string.IsNullOrEmpty(destinationEntityName) ||
                            string.IsNullOrEmpty(sourceRelType) || string.IsNullOrEmpty(destRelType) || string.IsNullOrEmpty(description))
                        {
                            errorMessage = "Relationship missing required fields: SourceEntityName, DestinationEntityName, SourceRelationshipType, DestinationRelationshipType, or Description.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!entityNames.Contains(sourceEntityName))
                        {
                            errorMessage = $"Relationship references invalid SourceEntityName '{sourceEntityName}'.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!entityNames.Contains(destinationEntityName))
                        {
                            errorMessage = $"Relationship references invalid DestinationEntityName '{destinationEntityName}'.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!ValidRelationshipTypes.Contains(sourceRelType))
                        {
                            errorMessage = $"Invalid SourceRelationshipType '{sourceRelType}'. Valid types are: {string.Join(", ", ValidRelationshipTypes)}.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }

                        if (!ValidRelationshipTypes.Contains(destRelType))
                        {
                            errorMessage = $"Invalid DestinationRelationshipType '{destRelType}'. Valid types are: {string.Join(", ", ValidRelationshipTypes)}.";
                            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                            goto ContinueAttempt;
                        }
                    }
                }

                // Deserialize JSON to ERDiagram
                var diagram = JsonSerializer.Deserialize<ERDiagram>(
                    jsonResult,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    }
                );

                if (diagram == null)
                {
                    errorMessage = "Deserialization returned null diagram.";
                    logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
                    continue;
                }

                logger.LogInformation("Attempt {attempt}: Generated diagram: {entityCount} entities, {relationshipCount} relationships",
                    attempt, diagram.Entites?.Count ?? 0, diagram.Relationships?.Count ?? 0);

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