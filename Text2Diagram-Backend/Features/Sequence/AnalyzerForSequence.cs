using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Features.Sequence.Components;

namespace Text2Diagram_Backend.Features.Sequence;

/// <summary>
/// Analyzes domain descriptions to extract elements for Entity Relationship Diagram (ERD) generation.
/// This analyzer is optimized for text describing entities and relationships.
/// </summary>
public class AnalyzerForSequence
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForSequence> logger;
    private const int MaxRetries = 3;
    public AnalyzerForSequence(Kernel kernel, ILogger<AnalyzerForSequence> logger)
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
	public async Task<SequenceDiagram> AnalyzeAsync(string domainDescription)
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
				var chatService = kernel.GetRequiredService<IChatCompletionService>();
				var chatHistory = new ChatHistory();
				chatHistory.AddUserMessage(prompt);

				var response = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
				var textContent = response.Content ?? "";
				logger.LogInformation("Attempt {attempt}: Response: {response}", attempt, textContent);

				// Extract JSON from ```json ... ``` or fallback to raw object
				string jsonResult = ExtractJsonFromText(textContent);
				if (string.IsNullOrWhiteSpace(jsonResult))
				{
					errorMessage = "Extracted JSON is empty or invalid.";
					logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
					continue;
				}

				logger.LogInformation("Attempt {attempt}: Extracted JSON: {json}", attempt, jsonResult);

				var jsonNode = JsonNode.Parse(jsonResult);
				if (jsonNode == null)
				{
					errorMessage = "Failed to parse JSON.";
					logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
					continue;
				}

				var elements = jsonNode["Elements"]?.AsArray();
				if (elements == null || elements.Count == 0)
				{
					errorMessage = "Missing or empty 'Elements' array.";
					logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
					continue;
				}

				foreach (var element in elements)
				{
					if (element == null)
						continue;

					if (element["Participant1"] != null && element["Participant2"] != null)
					{
						if (string.IsNullOrWhiteSpace(element["Message"]?.ToString()) || string.IsNullOrWhiteSpace(element["ArrowType"]?.ToString()))
						{
							errorMessage = "A basic message element is missing required fields: Message or ArrowType.";
							logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
							goto ContinueAttempt;
						}
					}
					else if (element["AltBlock"] != null)
					{
						var branches = element["AltBlock"]?["Branches"]?.AsArray();
						if (branches == null || branches.Count == 0)
						{
							errorMessage = "AltBlock missing or empty 'Branches'.";
							logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
							goto ContinueAttempt;
						}

						foreach (var branch in branches)
						{
							var condition = branch["Condition"]?.ToString();
							var body = branch["Body"]?.AsArray();

							if (string.IsNullOrWhiteSpace(condition) || body == null || body.Count == 0)
							{
								errorMessage = $"Branch missing 'Condition' or 'Body'.";
								logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
								goto ContinueAttempt;
							}

							foreach (var msg in body)
							{
								if (string.IsNullOrWhiteSpace(msg["Participant1"]?.ToString()) ||
									string.IsNullOrWhiteSpace(msg["Participant2"]?.ToString()) ||
									string.IsNullOrWhiteSpace(msg["Message"]?.ToString()) ||
									string.IsNullOrWhiteSpace(msg["ArrowType"]?.ToString()))
								{
									errorMessage = "Message inside AltBlock body missing required fields.";
									logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
									goto ContinueAttempt;
								}
							}
						}
					}
					else
					{
						errorMessage = "Element is neither a message nor an AltBlock.";
						logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
						goto ContinueAttempt;
					}
				}

				var diagram = JsonSerializer.Deserialize<SequenceDiagram>(
					jsonResult,
					new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true,
						AllowTrailingCommas = true,
						Converters = { new JsonStringEnumConverter() }
					}
				);

				if (diagram == null)
				{
					errorMessage = "Deserialization returned null.";
					logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
					continue;
				}

				logger.LogInformation("Attempt {attempt}: Sequence diagram contains {count} element(s).", attempt, diagram.Elements?.Count ?? 0);
				return diagram;

			ContinueAttempt:
				continue;
			}
			catch (JsonException ex)
			{
				errorMessage = $"JSON parsing error: {ex.Message}";
				logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
			}
			catch (Exception ex)
			{
				errorMessage = $"Unexpected error: {ex.Message}";
				logger.LogError(ex, "Attempt {attempt}: Unexpected error", attempt);
			}
		}

		logger.LogError("Failed to generate valid sequence diagram after {maxRetries} attempts.", MaxRetries);
		throw new FormatException($"Could not generate a valid sequence diagram after {MaxRetries} attempts.");
	}

	private string ExtractJsonFromText(string textContent)
	{
		var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
		if (codeFenceMatch.Success)
			return codeFenceMatch.Groups[1].Value.Trim();

		var rawJsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}", RegexOptions.Singleline);
		return rawJsonMatch.Success ? rawJsonMatch.Value.Trim() : "";
	}
	/// <summary>
	/// Generates a prompt for the LLM to extract ER diagram elements from a domain description.
	/// </summary>
	private string GetAnalysisPrompt(string domainDescription, string? errorMessage = null)
    {
        var prompt = $"""
            You are a Sequence Diagram Generator designed to analyze software behavior descriptions and convert them into structured object representations for Mermaid sequence diagrams. Your output will be used to automatically render Mermaid-compliant diagrams within the Text2Diagram_Backend.Features.Sequence namespace.

           ### TASK:
           Analyze the following use case and convert it into a structured sequence diagram object.

           Use Case:
           {domainDescription}

           ### INSTRUCTIONS:
           1. Identify participants (e.g., Client, Controller, Service, Repository) involved in the process.
           2. Break down the interactions between participants as message exchanges.
           3. Identify structured control blocks like:
              - loop: repeated interactions
              - alt / else: conditional logic
              - par / and: parallel execution
              - critical / option: critical sections with alternatives

           4. Your response must return a structured JSON object wrapped in code fences as follows:
           """
            +
		   """
            ```json
            {
              "Elements": [
                {
                  // Elements may include Statement, LoopBlock, AltBlock, ParallelBlock, CriticalBlock
                }
              ]
            }
            ```
            ### OBJECT SCHEMA:
            Root: SequenceDiagramSyntax
                Elements: List of SequenceElement
                    Statement:
                        Participant1 (string): sender
                        Participant2 (string): receiver
                        Message (string): message or call
                        ArrowType (string): one of [ "-->", "->>", "x->>", "--x", "-x" ]
                    LoopBlock:
                        Title (string)
                        Body (List<SequenceElement>)
                    AltBlock:
                        Branches (List<AltBranch>)
                    AltBranch:
                        Condition (string)
                        Body (List<SequenceElement>)
                    ParallelBlock:
                        Title (string)
                        Branches (List<ParallelBranch>)
                    ParallelBranch:
                        Title (string)
                        Body (List<SequenceElement>)
                    CriticalBlock:
                        Title (string)
                        Body (List<SequenceElement>)
                        Options (List<OptionBlock>)
                    OptionBlock:
                        Condition (string)
                        Body (List<SequenceElement>)
            """
			+
			"""
            ### EXAMPLE:
            INPUT:
            A user logs into a web app. They enter credentials into the UI, which are sent to the AuthController. The controller forwards the credentials to AuthService. The service checks the database using UserRepository. If credentials are valid, it returns success. Otherwise, it returns failure.
            OUTPUT:
            ```json
            {
              "Elements": [
                {
                  "Participant1": "User",
                  "Participant2": "UI",
                  "Message": "Enter credentials",
                  "ArrowType": "-->"
                },
                {
                  "Participant1": "UI",
                  "Participant2": "AuthController",
                  "Message": "Submit credentials",
                  "ArrowType": "->>"
                },
                {
                  "Participant1": "AuthController",
                  "Participant2": "AuthService",
                  "Message": "Validate credentials",
                  "ArrowType": "->>"
                },
                {
                  "Participant1": "AuthService",
                  "Participant2": "UserRepository",
                  "Message": "Query user by username/password",
                  "ArrowType": "->>"
                },
                {
                  "Participant1": "UserRepository",
                  "Participant2": "AuthService",
                  "Message": "Return user or null",
                  "ArrowType": "-->"
                },
                {
                  "Participant1": "AuthService",
                  "Participant2": "AuthController",
                  "Message": "Return result",
                  "ArrowType": "-->"
                },
                {
                  "Participant1": "AuthController",
                  "Participant2": "UI",
                  "Message": "Prepare login result",
                  "ArrowType": "-->"
                },
                {
                  "AltBlock": {
                    "Branches": [
                      {
                        "Condition": "Login successful",
                        "Body": [
                          {
                            "Participant1": "UI",
                            "Participant2": "User",
                            "Message": "Redirect to dashboard",
                            "ArrowType": "-->"
                          }
                        ]
                      },
                      {
                        "Condition": "Login failed",
                        "Body": [
                          {
                            "Participant1": "UI",
                            "Participant2": "User",
                            "Message": "Show error message",
                            "ArrowType": "-->"
                          }
                        ]
                      }
                    ]
                  }
                }
              ]
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