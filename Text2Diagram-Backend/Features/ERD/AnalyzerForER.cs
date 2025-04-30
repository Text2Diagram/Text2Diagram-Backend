using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;

namespace Text2Diagram_Backend.Features.ERD;

public class AnalyzerForER : IAnalyzer<ERDiagram>
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForER> logger;

    public AnalyzerForER(
        Kernel kernel,
        ILogger<AnalyzerForER> logger)
    {
        this.kernel = kernel;
        this.logger = logger;
    }

    public async Task<ERDiagram> AnalyzeAsync(string useCaseSpec)
    {
        try
        {
            var prompt = GetAnalysisPrompt(useCaseSpec);
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.AddUserMessage(prompt);
            var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: kernel);

            logger.LogInformation("LLM response: {response}", response);

            var diagram = ParseAndValidateResponse(response.Content ?? string.Empty);
            if (diagram == null)
            {
                logger.LogError("Failed to parse or validate the LLM response");
                throw new FormatException("Failed to analyze use case specification: invalid diagram structure.");
            }

            return diagram;

        }
        catch (Exception ex) when (ex is not FormatException)
        {
            logger.LogError(ex, "Unexpected error during use case specification analysis");
            throw new FormatException("Failed to analyze use case specification due to an internal error.", ex);
        }
    }

    private string GetAnalysisPrompt(string input)
    {
        return $"""
			You are an expert Entity Relationship Analyzer tasked with extracting and structuring input into ER diagram components.

			### TASK:
			Analyze the following input and structure it into a complete Entity Relationship representation:
			{input}

			""" +
            """
			### RULES FOR EXTRACTION AND STRUCTURING:
			1. **ENTITY**:
			   - Definition: A real-world object (e.g., physical like 'student' or conceptual like 'project').
			   - Attributes:
			     - `Name`: Uppercase, unique, no spaces (e.g., STUDENT).
			     - `Properties`: List of properties, each with:
			       - `Type`: Data type (e.g., string, int, float, string[]).
			       - `Name`: No spaces (e.g., studentId).
			       - `Role`: PK (primary key), FK (foreign key), or empty string if none.
			       - `Description`: Purpose of the property (e.g., "student identifier").
			
			2. **RELATIONSHIP**:
			- Definition: Describes connections between entities.
			- Attributes:
			  - `SourceEntityName`: Name of the source entity (from extracted entities).
			  - `DestinationEntityName`: Name of the destination entity (from extracted entities).
			  - `SourceRelationshipType`: Cardinality of source to destination (ZeroOrOne, ExactlyOne, ZeroOrMore, OneOrMore).
			  - `DestinationRelationshipType`: Cardinality of destination to source (ZeroOrOne, ExactlyOne, ZeroOrMore, OneOrMore).
			  - `Description`: Purpose of the relationship (e.g., "enrolls in").
				
			### OUTPUT FORMAT (JSON, all fields required):
			{{
			  "Entites": [
			    {{
			      "Name": "ENTITY_NAME",
			      "Properties": [
			        {{"Type": "string", "Name": "propName", "Role": "PK", "Description": "prop purpose"}}
			      ]
			    }}
			  ],
			  "Relationships": [
			    {{
			      "SourceEntityName": "ENTITY1",
			      "DestinationEntityName": "ENTITY2",
			      "SourceRelationshipType": "CardinalityType",
			      "DestinationRelationshipType": "CardinalityType",
			      "Description": "relationship purpose"
			    }}
			  ]
			}}

			### INSTRUCTIONS:
			- Identify entities from the input (e.g., Student, Course).
			- If properties aren’t provided, infer logical ones based on context (e.g., id as PK, name).
			- Define relationships based on entity interactions (e.g., "Student enrolls in Course").
			- Use only these cardinality types: ZeroOrOne, ExactlyOne, ZeroOrMore, OneOrMore.
			- Ensure entity names are uppercase, unique, and space-free; property names have no spaces.

			### EXAMPLE:
			**Input**: "A system to manage students and courses they enroll in."
			**Output**:
			{{
			  "Entites": [
			    {{
			      "Name": "STUDENT",
			      "Properties": [
			        {{"Type": "string", "Name": "id", "Role": "PK", "Description": "student identifier"}},
			        {{"Type": "string", "Name": "name", "Role": "", "Description": "student name"}}
			      ]
			    }},
			    {{
			      "Name": "COURSE",
			      "Properties": [
			        {{"Type": "string", "Name": "id", "Role": "PK", "Description": "course identifier"}},
			        {{"Type": "string", "Name": "title", "Role": "", "Description": "course title"}}
			      ]
			    }}
			  ],
			  "Relationships": [
			    {{
			      "SourceEntityName": "STUDENT",
			      "DestinationEntityName": "COURSE",
			      "SourceRelationshipType": "ZeroOrMore",
			      "DestinationRelationshipType": "ZeroOrMore",
			      "Description": "enrolls in"
			    }}
			  ]
			}}
			""";
    }

    private ERDiagram? ParseAndValidateResponse(string response)
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
                var result = JsonSerializer.Deserialize<ERDiagram>(json, serializerOptions);
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
