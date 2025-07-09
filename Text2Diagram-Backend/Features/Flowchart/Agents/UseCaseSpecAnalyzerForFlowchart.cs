using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.LLMServices;
using Text2Diagram_Backend.Middlewares;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class UseCaseSpecAnalyzerForFlowchart
{
    private readonly FlowCategorizer _flowCategorizer;
    private readonly BasicFlowExtractor _basicFlowExtractor;
    private readonly AlternativeFlowExtractor _alternativeFlowExtractor;
    private readonly ExceptionFlowExtractor _exceptionFlowExtractor;
    private readonly ILLMService _llmService;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly AiTogetherService _aiTogetherService;
    private readonly ILogger<UseCaseSpecAnalyzerForFlowchart> _logger;

    public UseCaseSpecAnalyzerForFlowchart(
        FlowCategorizer flowCategorizer,
        BasicFlowExtractor basicFlowExtractor,
        AlternativeFlowExtractor alternativeFlowExtractor,
        ExceptionFlowExtractor exceptionFlowExtractor,
        ILLMService llmService,
        IHubContext<ThoughtProcessHub> hubContext,
        AiTogetherService aiTogetherService,
        ILogger<UseCaseSpecAnalyzerForFlowchart> logger)
    {
        _flowCategorizer = flowCategorizer;
        _basicFlowExtractor = basicFlowExtractor;
        _alternativeFlowExtractor = alternativeFlowExtractor;
        _exceptionFlowExtractor = exceptionFlowExtractor;
        _llmService = llmService;
        _hubContext = hubContext;
        _aiTogetherService = aiTogetherService;
        _logger = logger;
    }

    public async Task<List<Flow>> AnalyzeAsync(string useCaseSpec)
    {
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting flows description from use case specification...");
        var (basicFlowDescription, alternativeFlowsDescription, exceptionFlowsDescription) = await _flowCategorizer.CategorizeFlowsAsync(useCaseSpec);

        var flowExtractTasks = new List<Task<Flow>>
        {
            _basicFlowExtractor.ExtractBasicFlowAsync(basicFlowDescription)
        };
        foreach (var alternativeFlowDescription in alternativeFlowsDescription)
        {
            flowExtractTasks.Add(_alternativeFlowExtractor.ExtractAlternativeFlowAsync(alternativeFlowDescription.Description, alternativeFlowDescription.Name));
        }

        foreach (var exceptionFlowDescription in exceptionFlowsDescription)
        {
            flowExtractTasks.Add(_exceptionFlowExtractor.ExtractExceptionFlowAsync(exceptionFlowDescription.Description, exceptionFlowDescription.Name));
        }


        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("StepGenerated", "Extracting Flows [The extraction of basic, alternative, and exception flows is executed in parallel]...");
        var result = await Task.WhenAll(flowExtractTasks);
        return result.ToList();
    }

    public async Task<string> GetDomainAsync(string useCaseSpec)
    {
        var domainPrompt = $"""
        Given the following use case specification: "{useCaseSpec}",
        identify the most relevant domain for this use case. Examples:
        - For "A user books a ride by selecting pickup and destination locations": "ride-hailing"
        - For "A user adds items to a cart and checks out with payment": "e-commerce"
        - For "A user transfers money between bank accounts": "banking"
        - For "A user schedules a medical appointment": "healthcare"
        If the domain is unclear, return "general".
        """
        +
        """
        Return JSON: { "Domain": "" }
        """;

        try
        {
            var domainResponse = await _aiTogetherService.GenerateContentAsync(domainPrompt);
            var domainJson = FlowchartHelpers.ValidateJson(domainResponse.Content);

            if (domainJson == null)
            {
                _logger.LogWarning("Invalid JSON response from LLM for use case domain. Defaulting to 'general'. Response: {Response}",
                    domainResponse.Content);
                return "general";
            }

            var domain = domainJson["Domain"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(domain))
            {
                _logger.LogWarning("LLM returned empty or invalid domain for use case spec. Defaulting to 'general'. Response: {Response}",
                    domainResponse.Content);
                return "general";
            }

            _logger.LogInformation("Determined domain '{Domain}' for use case specification.", domain);
            return domain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while determining domain for use case spec. Defaulting to 'general'.");
            return "general";
        }
    }
}