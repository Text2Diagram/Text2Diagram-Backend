namespace Text2Diagram_Backend.Data.Models;

public class Diagram
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public string DiagramData { get; set; } = string.Empty;
    public DiagramType DiagramType { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPublic { get; set; } = false;
    public string UserId { get; init; } = string.Empty;

    public ICollection<Share> Shares { get; set; } = [];

    private Diagram() { }

    public Diagram(string title, string diagramData, string userId, DiagramType diagramType, string? description)
    {
        Id = Guid.NewGuid();
        Title = title;
        DiagramData = diagramData;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UserId = userId;
        DiagramType = diagramType;
        IsPublic = true;
    }
}
