namespace Text2Diagram_Backend.Data.Models;

public class TempDiagram
{
    public Guid Id { get; init; }
    public string DiagramData { get; set; } = string.Empty;
    public DiagramType DiagramType { get; init; }
}
