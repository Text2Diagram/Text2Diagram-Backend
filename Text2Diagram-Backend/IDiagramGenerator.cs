namespace Text2Diagram_Backend;

public interface IDiagramGenerator
{
    Task<string> GenerateAsync(string input);
}
