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
using Text2Diagram_Backend.Features.Helper;
using Text2Diagram_Backend.Features.Sequence.Agent;
using Microsoft.AspNetCore.SignalR;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Middlewares;
using Text2Diagram_Backend.Features.Sequence.Agent.Objects;

namespace Text2Diagram_Backend.Features.Sequence;

/// <summary>
/// Analyzes domain descriptions to extract elements for Entity Relationship Diagram (ERD) generation.
/// This analyzer is optimized for text describing entities and relationships.
/// </summary>
public class AnalyzerForSequence
{
    private readonly Kernel kernel;
    private readonly ILogger<AnalyzerForSequence> logger;
    private const int MaxRetries = 1;
	private readonly ILLMService _llmService;
    private IHubContext<ThoughtProcessHub> _hubContext;
	public AnalyzerForSequence(Kernel kernel, ILogger<AnalyzerForSequence> logger, ILLMService llmService
        , IHubContext<ThoughtProcessHub> hubContext)
    {
        this.kernel = kernel;
        this.logger = logger;
		_llmService = llmService;
        _hubContext = hubContext;
	}
    /// <summary>
    /// Analyzes a domain description to generate an Entity Relationship Diagram.
    /// </summary>
    /// <param name="domainDescription">The domain description text to analyze.</param>
    /// <returns>An ER diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the domain description is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<string> AnalyzeAsync(string domainDescription)
    {
        string? errorMessage = null;
        List<string> listResultMermaidCode = new List<string?>();
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                //step_1 : pre-process the input
                //var chatService = kernel.GetRequiredService<IChatCompletionService>();
                //var chatHistory = new ChatHistory();
                var promtGetFlow = Step1_ParseFileToGetFlow.GenPromtInStep1(domainDescription);
				await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Pre-processing the input...");
				var responseGetFlowInStep1 = await _llmService.GenerateContentAsync(promtGetFlow);
				//chatHistory.AddUserMessage(promtGetFlow);
				//var responseGetFlowInStep1 = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                var textGetFlow = responseGetFlowInStep1.Content ?? "";
				// Extract JSON from the response
				var finalGetFlow = ExtractJsonFromTextHelper.ExtractJsonFromText(textGetFlow);
                var listUseCaseInGetFlow = DeserializeLLMResponseFunc.DeserializeLLMResponse<UseCaseDto>(finalGetFlow);
                foreach (var item in listUseCaseInGetFlow)
                {
                    //step_2 : Combine flows 
                    var promtCombineFlow = Step2_CombineFlow.CombineFlowsPromt(JsonConvert.SerializeObject(item));
                    //chatHistory.AddUserMessage(promtCombineFlow);
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Combining flows...");
                    var responseCombineFlow = await _llmService.GenerateContentAsync(promtCombineFlow);
                    var textContentCombineflow = responseCombineFlow.Content ?? "";
					// Extract JSON from the response
					var finalCombineFlow = ExtractJsonFromTextHelper.ExtractJsonFromText(textContentCombineflow);
                    var listCombineFlow = DeserializeLLMResponseFunc.DeserializeLLMResponse<UseCaseInputDto>(finalCombineFlow);
                    //step_3 : identify participants
                    var strCombineFlow = JsonConvert.SerializeObject(listCombineFlow.FirstOrDefault());
                    var promtIdentityParticipant = Step3_IdentifyParticipant.IdentifyParticipants(strCombineFlow);
                    //chatHistory.AddUserMessage(promtIdentityParticipant);
                    //var responseParticipants = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Identifying participants...");
                    var responseParticipants = await _llmService.GenerateContentAsync(promtIdentityParticipant);
					var textContentParticipants = responseParticipants.Content ?? "";
					var finalParticipants = ExtractJsonFromTextHelper.ExtractJsonFromText(textContentParticipants);
                    var listParticipants = DeserializeLLMResponseFunc.DeserializeLLMResponse<StepParticipantDto>(finalParticipants);
                    //step_4 : Identify conditions
                    var promtIdentifyConditions = Step4_IdentifyCondition.IdentifyCondition(strCombineFlow);
                    //chatHistory.AddUserMessage(promtIdentifyConditions);
                    //var responseConditions = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Identifying conditions...");
                    var responseConditions = await _llmService.GenerateContentAsync(promtIdentifyConditions);
                    var textContentConditions = responseConditions.Content ?? "";
					var finalConditions = ExtractJsonFromTextHelper.ExtractJsonFromText(textContentConditions);
                    var listConditions = DeserializeLLMResponseFunc.DeserializeLLMResponse<StepControlTypeDto>(finalConditions);
                    //step_5 : Combine LLM responses
                    var promtCombineLLMResponse = Step5_CombineLLMResult.CombineLLMResults(listCombineFlow.FirstOrDefault(), listParticipants, listConditions);
                    // step_6 : Generate mermaid syntax for sequence diagram

                    var promtFinalGenerateMermaid = Step6_GenerateMermaidCode.GenerateMermaidCode(promtCombineLLMResponse);
                    //chatHistory.AddUserMessage(promtFinalGenerateMermaid);
                    var responseMermaid = await _llmService.GenerateContentAsync(promtFinalGenerateMermaid);
                    var textReponseMermaid = responseMermaid.Content ?? "";
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generating sequence diagram...");
					//var finalMermaidCode = ExtractJsonFromText(textReponseMermaid);
					//var diagram = DeserializeToMermaicode(finalMermaidCode);
					var mermaidCode = StripMermaidFences(textReponseMermaid);
					// step_7 : Evaluate sequence diagram   
					var promtEvaluateSequenceDiagram = Step7_EvaluateSequenceDiagram.PromtEvaluateSequenceDiagram(domainDescription, mermaidCode);
					await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Evaluating sequence diagram...");
					var responseEvaluate = await _llmService.GenerateContentAsync(promtEvaluateSequenceDiagram);
					var textEvaluate = responseEvaluate.Content ?? "";
					var finalEvaluate = ExtractJsonFromTextHelper.ExtractJsonFromText(textEvaluate);
					var evaluateResult = DeserializeLLMResponseFunc.DeserializeLLMResponse<EvaluateResponseDto>(finalEvaluate);
					// Check if the evaluation indicates the diagram is accurate
					if (evaluateResult == null || evaluateResult.Count == 0 || evaluateResult[0].IsAccurate)
					{
                        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated sequence diagram successfully!");
                        // If the diagram is accurate, return it
                        return mermaidCode;
					}
					else
					{
						var promtValidate = PromtForRegenSequence.GetPromtForRegenSequence(JsonConvert.SerializeObject(evaluateResult[0]), mermaidCode);
						await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Modifying sequence diagram...");
						var responseValidate = await _llmService.GenerateContentAsync(promtValidate);
						var textValidate = responseValidate.Content ?? "";
                        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated sequence diagram successfully!");
                        return StripMermaidFences(textValidate);
					}
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
        return "";
    }


    public async Task<string> AnalyzeForRegenAsync(string feedback, string digramJson)
    {
        try
        {
			var promt = PromtForRegenSequence.GetPromtForRegenSequence(feedback, digramJson);
			//chatHistory.AddUserMessage(promtCombineFlow);
			var res = await _llmService.GenerateContentAsync(promt);
			var textContent = res.Content ?? "";
			// Extract JSON from the response
			//var final = ExtractJsonFromTextHelper.ExtractJsonFromText(textContent);
			return StripMermaidFences(textContent);
		}
        catch (Exception e)
        {
			logger.LogError(e.Message, "Attempt {attempt}: Unexpected error");
		}
        return "";
    }

	public static string StripMermaidFences(string input)
	{
		var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

		// Loại bỏ dòng đầu và dòng cuối
		if (lines.Length >= 3 && lines[0].Trim().Equals("```mermaid") && lines[^1].Trim().Equals("```"))
		{
			return string.Join("\n", lines.Skip(1).Take(lines.Length - 2)).Trim();
		}

		return input.Trim(); // Trả nguyên nếu không match
	}

    /// <summary>
    /// Analyzes a domain description to generate an Entity Relationship Diagram.
    /// </summary>
    /// <param name="domainDescription">The domain description text to analyze.</param>
    /// <returns>An ER diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the domain description is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    private SequenceDiagram DeserializeToMermaicode(string domainDescription)
    {
        var jsonNode = JsonNode.Parse(domainDescription);
		var updatedJson = jsonNode.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });
        var diagram = JsonConvert.DeserializeObject<SequenceDiagram>(
			updatedJson,
			new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				MissingMemberHandling = MissingMemberHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				Error = (sender, args) =>
				{
					logger.LogError("Deserialization error at {path}: {error}", args.ErrorContext.Path, args.ErrorContext.Error.Message);
					args.ErrorContext.Handled = true;
				}
			}
		);

		return diagram;
	}
}