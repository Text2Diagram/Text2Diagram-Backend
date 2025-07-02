namespace Text2Diagram_Backend.Common.Abstractions;

public interface IDiagramGenerator
{
    Task<DiagramContent> GenerateAsync(string input);
    Task<DiagramContent> ReGenerateAsync(string feedback, string diagramJson);
}

public class DiagramContent
{
    public string mermaidCode { get; set; }
    public string diagramJson { get; set; }
}
