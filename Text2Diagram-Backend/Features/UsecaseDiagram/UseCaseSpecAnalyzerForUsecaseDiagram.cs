using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Sequence.NewWay;
using Text2Diagram_Backend.Features.UsecaseDiagram.Separate;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.SignalR;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Middlewares;
using Text2Diagram_Backend.Features.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Text2Diagram_Backend.Features.ERD.Agents;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Features.Sequence.Agent.Objects;
using Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc;

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UseCaseSpecAnalyzerForUsecaseDiagram
{
    private readonly Kernel kernel;
    private readonly ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly ILLMService1 _llmService;

    public UseCaseSpecAnalyzerForUsecaseDiagram(
        Kernel kernel,
        ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger,
        IHubContext<ThoughtProcessHub> hubContext,
        ILLMService1 service)
    {
        this.kernel = kernel;
        this.logger = logger;
        _llmService = service;
        _hubContext = hubContext;
    }

    //public async Task<string> AnalyzeRequirementAsync(string requirement)
    //{
    //    var prompt = GetAnalysisRequirementPrompt(requirement);
    //    var response = await llm.GenerateAsync(prompt);
    //    var result = response.ToString().Trim();
    //    var json = "";
    //    if (result.Contains("```json"))
    //    {
    //        var parts = result.Split(new[] { "```json", "```" }, StringSplitOptions.RemoveEmptyEntries);
    //        json = parts[0];
    //    }

    //    logger.LogInformation("Use Case Analysis Response: {response}", json);

    //    return json;
    //}

    public async Task<UseCaseDiagram> AnalyzeAsync(string useCaseSpec)
    {
        try
        {
            //Get actors
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting actors from specification...");
            string promtActor = ExtractActor.GetExtractActorPrompt(useCaseSpec);
            var actorResult = await _llmService.GenerateContentAsync(promtActor);
            var actorJsonNode = Helpers.ValidateJson(actorResult.Content);
            var actors = ExtractActor.GetActors(actorJsonNode);

            //Get usecases
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting use cases from specification...");
            string promtUseCase = ExtractUsecase.GetExtractUseCasePrompt(useCaseSpec);
            var usecaseResult = await _llmService.GenerateContentAsync(promtUseCase);
            var usecaseJsonNode = Helpers.ValidateJson(usecaseResult.Content);
            var usecases = ExtractUsecase.GetUseCases(usecaseJsonNode);

            //Get associations
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting associations between actors and use cases...");
            string promtAssociation = ExtractAssociation.GetExtractAssociationPrompt(useCaseSpec, actors, usecases);
            var associationResult = await _llmService.GenerateContentAsync(promtAssociation);
            var associationJsonNode = Helpers.ValidateJson(associationResult.Content);
            var associations = ExtractAssociation.GetAssociations(associationJsonNode);

            // Get relationships
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting relationships between use cases...");
            string promptRelationship = ExtractRelationship.GetExtractRelationshipPrompt(useCaseSpec, usecases);
            var relationshipResult = await _llmService.GenerateContentAsync(promptRelationship);
            var relationshipJsonNode = Helpers.ValidateJson(relationshipResult.Content);
            var includes = ExtractRelationship.GetIncludes(relationshipJsonNode);
            var extends = ExtractRelationship.GetExtends(relationshipJsonNode);

            //Get packages
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting packages...");
            string? errorMessage = null;
            var combinedJson = new
            {
                Actors = actors.Select(a => new { a.Name }).ToList(),
                UseCases = usecases.Select(u => new { u.Name }).ToList(),
                Associations = associations.Select(a => new { a.Actor, a.UseCase }).ToList(),
                Includes = includes.Select(i => new { i.BaseUseCase, i.IncludedUseCase }).ToList(),
                Extends = extends.Select(e => new { e.BaseUseCase, e.ExtendedUseCase }).ToList()
            };

            string combinedJsonInput = System.Text.Json.JsonSerializer.Serialize(combinedJson, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string promptPackage = ExtractPackage.GetExtractPackagePrompt(combinedJsonInput);
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generating usecase diagram...");
            var packageResult = await _llmService.GenerateContentAsync(promptPackage);
            string jsonResult;
            var codeFenceMatch = Regex.Match(packageResult.Content, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            if (codeFenceMatch.Success)
            {
                jsonResult = codeFenceMatch.Groups[1].Value.Trim();
            }
            else
            {
                // Fallback to raw JSON
                var rawJsonMatch = Regex.Match(packageResult.Content, @"\{[\s\S]*\}", RegexOptions.Singleline);
                if (!rawJsonMatch.Success)
                {
                    errorMessage = "No valid JSON found in response. Expected JSON in code fences (```json ... ```) or raw JSON object.";
                    logger.LogWarning(" Raw response:\n{Content}", errorMessage);
                }
                jsonResult = rawJsonMatch.Value.Trim();
            }
            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                errorMessage = "Extracted JSON is empty.";
                logger.LogWarning(" Raw response:\n{Content}", errorMessage);
            }

            logger.LogInformation(" Raw response json:\n{Content}", jsonResult);

            var diagram = System.Text.Json.JsonSerializer.Deserialize<UseCaseDiagram>(jsonResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            if (diagram == null)
            {
                errorMessage = "Deserialization returned null.";
                logger.LogWarning(" Raw response ex:\n{Content}", errorMessage);
            }
            else
            {
                diagram.Packages.RemoveAll(p => p.Actors.Count == 0 || p.UseCases.Count == 0);
            }
            await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Evaluating usecase diagram...");
            var promptEvaluate = EvaluateUsecaseDiagram.PromptEvaluateUsecaseDiagram(useCaseSpec, jsonResult);
            var evaluationResult = await _llmService.GenerateContentAsync(promptEvaluate);
            var evaluationJsonResult = ExtractJsonFromTextHelper.ExtractJsonFromText(evaluationResult.Content);
            Console.WriteLine("Evaluation JSON Result: " + evaluationJsonResult);
            var evaluateResult = DeserializeLLMResponseFunc.DeserializeLLMResponse<EvaluateResponseDto>(evaluationJsonResult);
            if (evaluateResult == null || evaluateResult.Count == 0 || evaluateResult[0].IsAccurate)
            {
                await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated usecase diagram successfully!");
                return diagram;
            }
            else
            {
                await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Modifying usecase diagram...");
                diagram = await AnalyzeRegenAsync(JsonConvert.SerializeObject(evaluateResult[0]), jsonResult);
                await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Generated usecase diagram successfully!");
                return diagram;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing use case specification: {useCaseSpec}", ex);
            throw new InvalidOperationException("Failed to analyze use case specification.", ex);
        }

    }
    public async Task<UseCaseDiagram> AnalyzeRegenAsync(string feedback, string diagramJson)
    {
        var prompt = getPromptForRegen(feedback, diagramJson);
        var response = await _llmService.GenerateContentAsync(prompt);
        logger.LogInformation("Use Case Regeneration Response: {response}", response.Content);
        string errorMessage = string.Empty;
        string jsonResult = string.Empty;
        var codeFenceMatch = Regex.Match(response.Content, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (codeFenceMatch.Success)
        {
            jsonResult = codeFenceMatch.Groups[1].Value.Trim();
        }
        else
        {
            // Fallback to raw JSON
            var rawJsonMatch = Regex.Match(response.Content, @"\{[\s\S]*\}", RegexOptions.Singleline);
            if (!rawJsonMatch.Success)
            {
                errorMessage = "No valid JSON found in response. Expected JSON in code fences (```json ... ```) or raw JSON object.";
                logger.LogWarning(" Raw response:\n{Content}", errorMessage);
            }
            jsonResult = rawJsonMatch.Value.Trim();
        }
        var diagram = System.Text.Json.JsonSerializer.Deserialize<UseCaseDiagram>(jsonResult, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        if (diagram == null)
        {
            errorMessage = "Deserialization returned null.";
            logger.LogWarning(" Raw response ex:\n{Content}", errorMessage);
        }
        diagram.Packages.RemoveAll(p => p.Actors.Count == 0 || p.UseCases.Count == 0);
        return diagram;
    }


    private static string getPromptForRegen(string feedback, string diagramJson)
    {
        string prompt = $"""
        You are an expert assistant that helps regenerate a Use Case Diagram based on user feedback.

        You will receive:
        1. The current Use Case Diagram as JSON.
        2. A feedback message from the user that may suggest adding, removing, or updating actors, use cases, associations, packages, includes, or extends.

        Your task is to:
        - Analyze the feedback carefully.
        - Modify the given diagram accordingly.
        - Return a new, updated Use Case Diagram that reflects only the changes described in the feedback.
        - Do not remove or alter unrelated parts of the diagram.
        - Preserve the existing structure and content as much as possible.

        ---

        📌 INPUT FORMAT:

        🧾 Current Use Case Diagram:
        {diagramJson}

        🗣 User Feedback:
        {feedback}
        """
        + """
        ---

        📌 OUTPUT FORMAT:

        Return only the updated Use Case Diagram in the following JSON structure:

        ```json
        {
          "Packages": [
            {
              "Name": "BOUNDARY NAME",
              "Actors": [
                { "Name": "ACTOR_NAME_1" },
                { "Name": "ACTOR_NAME_2" }
              ],
              "UseCases": [
                { "Name": "USE CASE NAME 1" },
                { "Name": "USE CASE NAME 2" }
              ],
              "Associations": [
                { "Actor": "ACTOR_NAME", "UseCase": "USE CASE NAME" }
              ],
              "Includes": [
                { "BaseUseCase": "BASE USE CASE NAME", "IncludedUseCase": "INCLUDED USE CASE NAME" }
              ],
              "Extends": [
                { "BaseUseCase": "BASE USE CASE NAME", "ExtendedUseCase": "EXTENDED USE CASE NAME" }
              ]
            }
          ]
        }
        ```

        ✅ Your response must be a valid, minified JSON object of the updated diagram.
        ❌ Do not explain, comment, or return markdown formatting (e.g., no ```json block).
        ❌ Do not return unchanged diagram unless the feedback explicitly says ""no change"".
        """;
        return prompt;
    }
}

