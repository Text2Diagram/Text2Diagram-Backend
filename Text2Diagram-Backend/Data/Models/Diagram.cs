namespace Text2Diagram_Backend.Data.Models;

public class Diagram
{
    public Guid Id { get; init; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public string DiagramData { get; set; } = string.Empty;
    public DiagramType DiagramType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public string UserId { get; init; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string DiagramJson { get; set; } = string.Empty;

    private Diagram() { }
}
