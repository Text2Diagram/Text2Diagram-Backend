namespace Text2Diagram_Backend.Common.Abstractions;

public interface IDiagramGenerator
{
    Task<string> GenerateAsync(string input);
}
