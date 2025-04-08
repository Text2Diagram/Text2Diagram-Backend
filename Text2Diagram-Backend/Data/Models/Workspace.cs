namespace Text2Diagram_Backend.Data.Models;

public class Workspace
{
    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public ICollection<Diagram> Diagrams { get; set; } = [];
    public ICollection<WorkspaceMember> Members { get; set; } = [];

    private Workspace() { }

    public Workspace(string name, string ownerId, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        OwnerId = ownerId;
        Description = description;
    }
}
