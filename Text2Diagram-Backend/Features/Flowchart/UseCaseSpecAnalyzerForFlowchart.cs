using System.Text.Json;
using Text2Diagram_Backend.Features.Flowchart.Agents;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Analyzes structured use case specifications to extract elements for flowchart generation.
/// This analyzer is optimized for structured text following use case specification format.
/// </summary>
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

    /// <summary>
    /// Analyzes a structured use case specification to generate a flowchart diagram.
    /// </summary>
    /// <param name="useCaseSpec">The use case specification text to analyze.</param>
    /// <returns>A flowchart diagram ready for rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when the use case specification is empty.</exception>
    /// <exception cref="FormatException">Thrown when analysis fails to extract valid diagram elements.</exception>
    public async Task<FlowchartDiagram> AnalyzeAsync(string useCaseSpec)
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
        var basicFlow = result.First(f => string.IsNullOrEmpty(f.Name));
        var others = result.Where(f => !string.IsNullOrEmpty(f.Name)).ToList();

        _logger.LogInformation("Basic flow: {basicFlow}", JsonSerializer.Serialize(basicFlow));

        foreach (var flow in others)
        {

            _logger.LogInformation("Sub flow {subFlowName}: {subFlow}", flow.Name, JsonSerializer.Serialize(flow));
        }

        return new FlowchartDiagram(basicFlow, others);
    }
}