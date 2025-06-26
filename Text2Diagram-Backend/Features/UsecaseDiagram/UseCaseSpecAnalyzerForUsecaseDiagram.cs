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

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UseCaseSpecAnalyzerForUsecaseDiagram
{
    private readonly Kernel kernel;
    private readonly ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger;
    private readonly ILLMService _llmService;

    public UseCaseSpecAnalyzerForUsecaseDiagram(
        Kernel kernel,
        ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger,
        ILLMService service)
    {
        this.kernel = kernel;
        this.logger = logger;
        _llmService = service;
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
            string promtActor = ExtractActor.GetExtractActorPrompt(useCaseSpec);
            var actorResult = await _llmService.GenerateContentAsync(promtActor);
            var actorJsonNode = Helpers.ValidateJson(actorResult.Content);
            var actors = ExtractActor.GetActors(actorJsonNode);

            //Get usecases
            string promtUseCase = ExtractUsecase.GetExtractUseCasePrompt(useCaseSpec);
            var usecaseResult = await _llmService.GenerateContentAsync(promtUseCase);
            var usecaseJsonNode = Helpers.ValidateJson(usecaseResult.Content);
            var usecases = ExtractUsecase.GetUseCases(usecaseJsonNode);

            //Get associations
            string promtAssociation = ExtractAssociation.GetExtractAssociationPrompt(useCaseSpec, actors, usecases);
            var associationResult = await _llmService.GenerateContentAsync(promtAssociation);
            var associationJsonNode = Helpers.ValidateJson(associationResult.Content);
            var associations = ExtractAssociation.GetAssociations(associationJsonNode);

            // Get relationships
            string promptRelationship = ExtractRelationship.GetExtractRelationshipPrompt(useCaseSpec, usecases);
            var relationshipResult = await _llmService.GenerateContentAsync(promptRelationship);
            var relationshipJsonNode = Helpers.ValidateJson(relationshipResult.Content);
            var includes = ExtractRelationship.GetIncludes(relationshipJsonNode);
            var extends = ExtractRelationship.GetExtends(relationshipJsonNode);

            //Get packages
            string? errorMessage = null;
            var combinedJson = new
            {
                Actors = actors.Select(a => new { a.Name }).ToList(),
                UseCases = usecases.Select(u => new { u.Name }).ToList(),
                Associations = associations.Select(a => new { a.Actor, a.UseCase }).ToList(),
                Includes = includes.Select(i => new { i.BaseUseCase, i.IncludedUseCase }).ToList(),
                Extends = extends.Select(e => new { e.BaseUseCase, e.ExtendedUseCase }).ToList()
            };

            string combinedJsonInput = JsonSerializer.Serialize(combinedJson, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string promptPackage = ExtractPackage.GetExtractPackagePrompt(combinedJsonInput);
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

            logger.LogInformation(" Raw response:\n{Content}", jsonResult);

            var diagram = JsonSerializer.Deserialize<UseCaseDiagram>(jsonResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            if (diagram == null)
            {
                errorMessage = "Deserialization returned null.";
                logger.LogWarning(" Raw response:\n{Content}", errorMessage);
            }

            return diagram;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing use case specification: {useCaseSpec}", ex);
            throw new InvalidOperationException("Failed to analyze use case specification.", ex);
        }

    }

}

