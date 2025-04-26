using System.Security.Cryptography.X509Certificates;
using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

/// <summary>
/// Generates flowchart diagrams in Mermaid.js format from structured data
/// extracted from use case specifications or natural language text.
/// </summary>
public class ERDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<ERDiagramGenerator> logger;
    private readonly IAnalyzer<ERDiagram> analyzer;

    public ERDiagramGenerator(
        ILogger<ERDiagramGenerator> logger,
        IAnalyzer<ERDiagram> analyzer)
    {
        this.logger = logger;
        this.analyzer = analyzer;
    }

    /// <summary>
    /// Generates a flowchart diagram in Mermaid.js format from text input.
    /// </summary>
    /// <param name="input">Use case specifications, BPMN files, or natural language text.</param>
    /// <returns>Generated Mermaid code for Flowchart Diagram</returns>
    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeAsync(input);

            // Generate Mermaid syntax
            string mermaidCode = GenerateMermaidCode(diagram);

            logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", mermaidCode);

            // Validate and correct if needed
            return mermaidCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

    /// <summary>
    /// Generates Mermaid.js compatible syntax for the flowchart diagram.
    /// </summary>
    private string GenerateMermaidCode(ERDiagram diagram)
    {
        string result = "erDiagram \n";
		foreach (var entity in diagram.entities)
		{
            result += $"{entity.name}" + "{\n";
            foreach(var prop in entity.properties)
            {
                result += $"{prop.type} {prop.name.Trim().Replace(' ', '_')} {prop.role} {prop.description} \n";
            }
            result += "}\n";    
		}
		foreach (var relation in diagram.relationships)
        {
			char[] nameArray = GetEdgeConnector(relation.destination_relationship_name).ToCharArray();
			Array.Reverse(nameArray);
			string reverse = new string(nameArray);
			result += $"{relation.source_entity_name} {GetEdgeConnector(relation.source_relationship_name)}--{reverse} {relation.destination_entity_name}" +
                $" : {relation.decription}\n";
        }
        return result;
    }

	private string GetEdgeConnector(RelationshipType type)
	{
		return type switch
		{
			RelationshipType.ZeroOrOne => "|o", //Zero or one
			RelationshipType.ExactlyOne => "||", //	Exactly one
			RelationshipType.ZeroOrMore => "}o", //Zero or more (no upper limit)
			RelationshipType.OneOrMore => "}|", //One or more (no upper limit)
			// Default fallback
			_ => "|o"
		};
	}
}
