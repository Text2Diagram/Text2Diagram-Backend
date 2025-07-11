using Microsoft.AspNetCore.SignalR;

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
    private readonly ILLMService1 _llmService;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly AiTogetherService _aiTogetherService;
    private readonly ILogger<UseCaseSpecAnalyzerForFlowchart> _logger;

    public UseCaseSpecAnalyzerForFlowchart(
        FlowCategorizer flowCategorizer,
        BasicFlowExtractor basicFlowExtractor,
        AlternativeFlowExtractor alternativeFlowExtractor,
        ExceptionFlowExtractor exceptionFlowExtractor,
        ILLMService1 llmService,
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

}