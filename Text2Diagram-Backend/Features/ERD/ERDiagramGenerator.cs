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
            var diagram = await analyzer.AnalyzeAsync(input);

            // Generate Mermaid syntax
            string mermaidCode = GenerateMermaidCode(diagram);

            logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", mermaidCode);

            return new DiagramContent
            {
                mermaidCode = mermaidCode,
				diagramJson = JsonConvert.SerializeObject(diagram)
			};
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
			var diagram = await analyzer.AnalyzeForReGenAsync(feedback, diagramJson);

			// Generate Mermaid syntax
			string mermaidCode = GenerateMermaidCode(diagram);

			logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", mermaidCode);

			return new DiagramContent
			{
				mermaidCode = mermaidCode,
				diagramJson = JsonConvert.SerializeObject(diagram)
			};
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
	private string GenerateMermaidCode(ERDiagram diagram)
    {
        var mermaid = new StringBuilder();

        // Start ER diagram definition
        mermaid.AppendLine("erDiagram");

        foreach (var entity in diagram.Entites)
        {
            mermaid.AppendLine($"    {entity.Name} {{");
            foreach (var prop in entity.Properties)
            {
                mermaid.AppendLine($"        {prop.Type} {prop.Name.Trim().Replace(' ', '_')} {prop.Role} \"{prop.Description}\"");
            }
            mermaid.AppendLine("    }");
        }

        foreach (var relation in diagram.Relationships)
        {
            string sourceConnector = GetEdgeConnector(relation.DestinationRelationshipType, true);
            string destConnector = GetEdgeConnector(relation.SourceRelationshipType, false);
            mermaid.AppendLine($"    {relation.SourceEntityName} {sourceConnector}--{destConnector} {relation.DestinationEntityName} : \"{relation.Description}\"");
        }

        return mermaid.ToString();
    }

    /// <summary>
    /// Determines the appropriate connector symbol for a relationship type.
    /// </summary>
    /// <param name="type">The relationship type to evaluate</param>
    /// <param name="isLeft">Indicates if this is the left (source) side of the relationship</param>
    /// <returns>A string representing the Mermaid.js connector symbol</returns>
    private string GetEdgeConnector(RelationshipType type, bool isLeft)
    {
        return type switch
        {
            RelationshipType.ZeroOrOne => isLeft ? "|o" : "o|",
            RelationshipType.ExactlyOne => "||",
            RelationshipType.ZeroOrMore => isLeft ? "}o" : "o{",
            RelationshipType.OneOrMore => isLeft ? "}|" : "|{",
            _ => isLeft ? "|o" : "o|"
        };
    }
}