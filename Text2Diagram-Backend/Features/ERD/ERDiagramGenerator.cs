using Newtonsoft.Json;
using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Migrations;

namespace Text2Diagram_Backend.Features.ERD;


public class ERDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<ERDiagramGenerator> logger;
    private readonly AnalyzerForER analyzer;

    public ERDiagramGenerator(
        ILogger<ERDiagramGenerator> logger,
        AnalyzerForER analyzer)
    {
        this.logger = logger;
        this.analyzer = analyzer;
    }

    public async Task<DiagramContent> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var result = await analyzer.AnalyzeAsync(input);

            logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", result.mermaidCode);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

	public async Task<DiagramContent> ReGenerateAsync(string feedback, string diagramJson)
	{
		try
		{
			// Extract and generate diagram structure directly from input
			var result = await analyzer.AnalyzeForReGenAsync(feedback, diagramJson);

			logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", result.mermaidCode);

			return result;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error generating flowchart diagram");
			throw;
		}
	}

	/// <summary>
	/// Generates Mermaid.js compatible syntax for the ER diagram.
	/// </summary>
	/// <param name="diagram">The ER diagram object containing entities and relationships</param>
	/// <returns>A string containing Mermaid.js ER diagram syntax</returns>
}