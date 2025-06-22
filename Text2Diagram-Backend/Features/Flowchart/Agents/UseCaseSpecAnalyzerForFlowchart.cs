using System.Text.Json;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class UseCaseSpecAnalyzerForFlowchart
{
    private readonly FlowCategorizer _flowCategorizer;
    private readonly BasicFlowExtractor _basicFlowExtractor;
    private readonly AlternativeFlowExtractor _alternativeFlowExtractor;
    private readonly ExceptionFlowExtractor _exceptionFlowExtractor;
    private readonly ILogger<UseCaseSpecAnalyzerForFlowchart> _logger;

    public UseCaseSpecAnalyzerForFlowchart(
        FlowCategorizer flowCategorizer,
        BasicFlowExtractor basicFlowExtractor,
        AlternativeFlowExtractor alternativeFlowExtractor,
        ExceptionFlowExtractor exceptionFlowExtractor,
        ILogger<UseCaseSpecAnalyzerForFlowchart> logger)
    {
        _flowCategorizer = flowCategorizer;
        _basicFlowExtractor = basicFlowExtractor;
        _alternativeFlowExtractor = alternativeFlowExtractor;
        _exceptionFlowExtractor = exceptionFlowExtractor;
        _logger = logger;
    }

    public async Task<List<Flow>> AnalyzeAsync(string useCaseSpec)
    {
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

        var result = await Task.WhenAll(flowExtractTasks);
        foreach (var flow in result)
        {

            _logger.LogInformation("[{flowType}] {flowName}: {flow}", flow.FlowType.ToString(), flow.Name, JsonSerializer.Serialize(flow));
        }

        return result.ToList();
    }
}