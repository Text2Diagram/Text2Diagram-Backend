namespace Text2Diagram_Backend.Abstractions;

public interface IDiagramGenerator
{
    Task<string> GenerateAsync(string input);
}
