namespace Text2Diagram_Backend.Data.Models;

public class WorkspaceMember
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; set; }
    public string UserId { get; init; } = string.Empty;
    public WorkspaceRole Role { get; init; }

    private WorkspaceMember() { }

    public WorkspaceMember(Guid workspaceId, string userId, WorkspaceRole role)
    {
        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
    }
}
