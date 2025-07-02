namespace Text2Diagram_Backend.Common.Abstractions;

public interface IDiagramGenerator
{
    Task<object> GenerateAsync(string input);
    Task<object> ReGenerateAsync(string feedback, string diagramJson);
}
