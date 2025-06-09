using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Analyzes structured use case specifications to extract elements for flowchart generation.
/// This analyzer is optimized for structured text following use case specification format.
/// </summary>
public class UseCaseSpecAnalyzerForFlowchart
{
    private readonly BasicFlowExtractor _basicFlowExtractor;

    public UseCaseSpecAnalyzerForFlowchart(BasicFlowExtractor basicFlowExtractor)
    {
        _basicFlowExtractor = basicFlowExtractor;
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
        var basicFlowDescription = useCaseSpec;

        var basicFlow = await _basicFlowExtractor.ExtractBasicFlowAsync(basicFlowDescription);
        return new FlowchartDiagram(basicFlow, new List<SubFlow>());
    }

}