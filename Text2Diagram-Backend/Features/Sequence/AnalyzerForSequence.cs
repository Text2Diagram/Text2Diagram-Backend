using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.Sequence.Components;
using Newtonsoft.Json.Converters;
using System.Text.Json;
using Text2Diagram_Backend.Features.Sequence.NewWay;
using Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;
using DocumentFormat.OpenXml.VariantTypes;
using Text2Diagram_Backend.Common.Abstractions;
using Azure.Core;

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
	private readonly ILLMService _llmService;
	public AnalyzerForSequence(Kernel kernel, ILogger<AnalyzerForSequence> logger, ILLMService llmService)
    {
        this.kernel = kernel;
        this.logger = logger;
		_llmService = llmService;
	}
	/// <summary>
	/// Analyzes a domain description to generate an Entity Relationship Diagram.
	/// </summary>
	/// <param name="domainDescription">The domain description text to analyze.</param>
	/// <returns>An ER diagram ready for rendering.</returns>
	/// <exception cref="ArgumentException">Thrown when the domain description is empty.</exception>
	/// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
	public async Task<List<string>> AnalyzeAsync(string domainDescription)
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
				//step_1 : pre-process the input
				var chatService = kernel.GetRequiredService<IChatCompletionService>();
				var chatHistory = new ChatHistory();
				List<string> listResultMermaidCode = new List<string>();
				var promtGetFlow = Step1_ParseFileToGetFlow.GenPromtInStep1(domainDescription);
				chatHistory.AddUserMessage(promtGetFlow);
				var responseGetFlowInStep1 = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
				var textGetFlow = responseGetFlowInStep1.Content ?? "";
				// Extract JSON from the response
				var finalGetFlow = ExtractJsonFromText(textGetFlow);
				var listUseCaseInGetFlow = DeserializeLLMResponseFunc.DeserializeLLMResponse<UseCaseDto>(finalGetFlow);
				foreach (var item in listUseCaseInGetFlow)
				{
					//step_2 : Combine flows 
					var promtCombineFlow = Step2_CombineFlow.CombineFlowsPromt(JsonConvert.SerializeObject(item));
					chatHistory.AddUserMessage(promtCombineFlow);
					var responseCombineFlow = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					var textContentCombineflow = responseCombineFlow.Content ?? "";
					// Extract JSON from the response
					var finalCombineFlow = ExtractJsonFromText(textContentCombineflow);
					var listCombineFlow = DeserializeLLMResponseFunc.DeserializeLLMResponse<UseCaseInputDto>(finalCombineFlow);
					//step_3 : identify participants
					var strCombineFlow = JsonConvert.SerializeObject(listCombineFlow.FirstOrDefault());
					var promtIdentityParticipant = Step3_IdentifyParticipant.IdentifyParticipants(strCombineFlow);
					chatHistory.AddUserMessage(promtIdentityParticipant);
					var responseParticipants = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					var textContentParticipants = responseParticipants.Content ?? "";
					var finalParticipants = ExtractJsonFromText(textContentParticipants);
					var listParticipants = DeserializeLLMResponseFunc.DeserializeLLMResponse<StepParticipantDto>(finalParticipants);
					//step_4 : Identify conditions
					var promtIdentifyConditions = Step4_IdentifyCondition.IdentifyCondition(strCombineFlow);
					chatHistory.AddUserMessage(promtIdentifyConditions);
					var responseConditions = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					var textContentConditions = responseConditions.Content ?? "";
					var finalConditions = ExtractJsonFromText(textContentConditions);
					var listConditions = DeserializeLLMResponseFunc.DeserializeLLMResponse<StepControlTypeDto>(textContentConditions);
					//step_5 : Combine LLM responses
					var promtCombineLLMResponse = Step5_CombineLLMResult.CombineLLMResults(listCombineFlow.FirstOrDefault(), listParticipants, listConditions);
					// step_6 : Generate mermaid syntax for sequence diagram
					var promtFinalGenerateMermaid = Step6_GenerateMermaidCode.GenerateMermaidCode(JsonConvert.SerializeObject(promtCombineLLMResponse));
					chatHistory.AddUserMessage(promtFinalGenerateMermaid);
					var responseMermaid = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					var textReponseMermaid = responseMermaid.Content ?? "";
					var finalMermaidCode = ExtractJsonFromText(textReponseMermaid);
					listResultMermaidCode.Add(finalMermaidCode);
					return listResultMermaidCode;
				}

			}
			catch (Newtonsoft.Json.JsonException ex)
			{
				errorMessage = $"JSON parsing error: {ex.Message}";
				logger.LogWarning("Attempt {attempt}: {error}", attempt, ex);
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


	//public async Task<List<string>> AnalyzeAsync(string domainDescription)
	//{
	//	string promtFlow = Step1_ParseFileToGetFlow.GenPromtInStep1(domainDescription);
	//	var result = await _llmService.GenerateContentAsync(promtFlow);
	//	var final = new List<string>();
	//	final.Add(result.Content);
	//	return final;
	//}

	private string ExtractJsonFromText(string textContent)
	{
		// Ưu tiên tìm trong code block có đánh dấu ```json
		var codeFenceMatch = Regex.Match(textContent, @"```(?:json)?\s*([\s\S]+?)\s*```", RegexOptions.Singleline);
		if (codeFenceMatch.Success)
		{
			return codeFenceMatch.Groups[1].Value.Trim();
		}

		// Nếu không có code fence, tìm JSON array hoặc object
		var arrayMatch = Regex.Match(textContent, @"\[\s*{[\s\S]*?}\s*\]", RegexOptions.Singleline);
		if (arrayMatch.Success)
		{
			return arrayMatch.Value.Trim();
		}

		var objectMatch = Regex.Match(textContent, @"{[\s\S]*}", RegexOptions.Singleline);
		if (objectMatch.Success)
		{
			return objectMatch.Value.Trim();
		}

		return "";
	}



	private string GetAnalysisPrompt(string domainDescription, string? errorMessage = null)
    {
        var prompt = $"""
			You are a Sequence Diagram Generator designed to analyze software behavior descriptions and convert them into structured object representations for Mermaid sequence diagrams. Your output will be used to automatically render Mermaid-compliant diagrams within the Text2Diagram_Backend.Features.Sequence namespace.

			### TASK:
			Analyze the following domain description and convert it into a structured sequence diagram object.

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