using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Analyzes structured use case specifications to extract elements for flowchart generation.
/// This analyzer is optimized for structured text following use case specification format.
/// </summary>
public class UseCaseSpecAnalyzerForER : IAnalyzer<ERDiagram>
{
	private readonly Kernel kernel;
	private readonly ILogger<UseCaseSpecAnalyzerForER> logger;

	public UseCaseSpecAnalyzerForER(
		Kernel kernel,
		ILogger<UseCaseSpecAnalyzerForER> logger)
	{
		this.kernel = kernel;
		this.logger = logger;
	}

	/// <summary>
	/// Analyzes a structured use case specification to extract and generate a flowchart diagram directly.
	/// </summary>
	/// <param name="useCaseSpec">The use case specification text to analyze.</param>
	/// <returns>A flowchart diagram ready for rendering.</returns>
	/// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
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

	/// <summary>
	/// Generates a prompt for the LLM to extract flowchart elements from a use case specification.
	/// </summary>
	private string GetAnalysisPrompt(string useCaseSpec)
	{
		return $"""
            You are an expert Entity Realationship Analyzer that extracts and structures customer input into diagram-ready components.

            ### TASK: Extract, organize, and structure the following below input into a complete Entity relationship representation:
            {useCaseSpec}

            """ +
			"""
            ### EXTRACTION AND STRUCTURING RULES FOR TWO COMPONENTS:

            1. ENTITY:  An entity is a real world object. For example: a car, an invoice, an employee, …
                        2 main types
                        ● A physical, observable object
                        – a student, a building, a car, …
                        ● A non-visual conceptual object
                        – a company, a project, a department, …
                It has the following attributes:
                    - name : entity name (must be uppercase and unique)
                    - propperties : list of entity properties, each element is a separate property of the entity, carries atomic values ​​and is structured as follows:
                            + type : data type of the property (string, int, double, decimal, float, string[], ...)
                            + name : property name(Does not contain spaces )
                            + role : role of the property if none then leave blank : (PK(primary key), FK(foreign key),..) 
                            + description : description of the purpose of the property
                            for example: teacher is an entity with the teacher code attribute with: {"type": "string", "name": "code", "role": "PK", "decription": "identifier for student"}
            
            2. RELATIONSHIP:
                The relationship part of each statement can be broken down into three sub-components:
             - the cardinality of the first entity with respect to the second
             - whether the relationship confers identity on a 'child' entity
             - the cardinality of the second entity with respect to the first
                It has the following attributes:
                + source_entity_name : name entity 1 (must be taken from the list of entities analyzed above)
                + destination_entity_name : name entity 2(must be taken from the list of entities analyzed above)
                + source_relationship_name : relationship name of entity 1 to entity 2 ()
                + destination_relationship_name : relationship name of entity 2 to entity 1
                + decription : decription of 
                ### NOTES:
                You are allowed to use only the following values ​​for source_relationship_name and destination_relationship_name
                    - ZeroOrOne: Zero or one
                    - ExactlyOne: Exactly one
                    - ZeroOrMore: Zero or more (no upper limit)        
                    - OneOrMore: ne or more (no upper limit)
                ### EXAMPLE: Consider teachers and subjects for the role of department head
                => RELATIONSHIP {
                        source_entity_name : TEACHER,
                        destination_entity_name : SUBJECT,
                        source_relationship_name : ZeroOrOne // because teachers are only allowed to be head of one subject or none at all.
                        destination_relationship_name : ExactlyOne // Because a department must have one teacher as the head of the department, it is not allowed to have no one as the head of the department.
                        decription : department head // The name of this relationship is "department head"
                   }
            

            OUTPUT JSON STRUCTURE (all fields required):
            {
              "entities": [
                {
                    "name": "TEACHER",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for teacher"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of teacher"},
                        {"type": "int", "name": "age", "role": "", "decription" : "age of teacher"},
                        {"type": "string[]", "name": "achivement", "role": "", "decription" : "achivement of teacher"},
                    ]
                },
                {
                    "name": "SUBJECT",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for SUBJECT"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of subject"},
                        {"type" : "int", "name" : "establishedYear", "role" : "", "decription" : "established yead of subject"}
                    ]
                },
                {
                    "name": "SCHOOL",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for SCHOOL"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of SHOOL"},
                        {"type" : "int", "name" : "establishedYear", "role" : "", "decription" : "established yead of SCHOOL"},
                        {"type" : "float", "name" : "qualityRating", "role" : "", "decription" : "Regional quality rating"}
                    ]
                },
              ],
              "relationships": [
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "ZeroOrOne", 
                    "destination_relationship_name": "ExactlyOne", 
                    "decription": "Department head"
                },
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SCHOOL", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "ZeroOrMore", 
                    "decription": "Contains"
                },
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "OneOrMore", 
                    "decription": "teach"
                },
                {
                    "source_entity_name": "SCHOOL", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "ZeroOrMore", 
                    "decription": "contains"
                }
              ]
            }

            EXAMPLE CONVERSION:
            INPUT: "Hi, I need to build a simple school management application, focusing on three main entities: School, Teacher, and Subject."


            You need the following analysis to easily solve the problem on "INPUT":
             - First identify the entity appearing in "INPUT" : SCHOOL, TEACHER, SUBJECT
             - Next, if the problem does not provide your attributes, use your experience and wisdom to determine the attributes for the entities you just identified.
                SCHOOL : code (identifier for SCHOOL) , name (name of school), establishedYear (established yead of SCHOOL), qualityRating (Regional quality rating)
                TEACHER : code (identifier for teacher), name (name of teacher), achivement (achivement of teacher)
                SUBJECT :  code (identifier for subject), name (name of subject), establishedYear (established year of subject)
             - Next, here you have to be a little more creative to define the possible relationships of the above entities:
                 + teacher who is the head of a subject
                 + teacher who teaches this subject
                 + how many subjects does the school contain
                 + how many teachers does the school contain
                 => there are four relationships as above
            ###After completing the above steps, you will easily get to the output below

            OUTPUT:
            {
              "entities": [
                {
                    "name": "TEACHER",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for teacher"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of teacher"},
                        {"type": "int", "name": "age", "role": "", "decription" : "age of teacher"},
                        {"type": "string[]", "name": "achivement", "role": "", "decription" : "achivement of teacher"},
                    ]
                },
                {
                    "name": "SUBJECT",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for SUBJECT"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of subject"},
                        {"type" : "int", "name" : "establishedYear", "role" : "", "decription" : "established yead of subject"}
                    ]
                },
                {
                    "name": "SCHOOL",
                    "properties": [
                        {"type": "string", "name": "code", "role": "PK", "decription" : "identifier for SCHOOL"},
                        {"type": "string", "name": "name", "role": "", "decription" : "name of SHOOL"},
                        {"type" : "int", "name" : "establishedYear", "role" : "", "decription" : "established yead of SCHOOL"},
                        {"type" : "float", "name" : "qualityRating", "role" : "", "decription" : "Regional quality rating"}
                    ]
                },
              ],
              "relationships": [
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "ZeroOrOne", 
                    "destination_relationship_name": "ExactlyOne", 
                    "decription": "Department head"
                },
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SCHOOL", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "ZeroOrMore", 
                    "decription": "Contains"
                },
                {
                    "source_entity_name": "TEACHER", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "OneOrMore", 
                    "decription": "teach"
                },
                {
                    "source_entity_name": "SCHOOL", 
                    "destination_entity_name": "SUBJECT", 
                    "source_relationship_name": "OneOrMore", 
                    "destination_relationship_name": "ZeroOrMore", 
                    "decription": "contains"
                }
              ]
            }

            EXTREMELY IMPORTANT:
             - entity name must be uppercase,  unique and does not contain spaces
             - property name(Does not contain spaces )
            """;
	}

	/// <summary>
	/// Parses and validates the LLM response to extract the flowchart diagram.
	/// </summary>
	/// <param name="response">The raw response from the LLM.</param>
	/// <returns>The extracted flowchart diagram, or null if extraction failed.</returns>
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
