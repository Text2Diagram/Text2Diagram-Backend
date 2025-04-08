namespace Text2Diagram_Backend.Data.Models;

public class Share
{
    public Share(Guid diagramId, string userId, SharePermission permission)
    {
        Id = Guid.NewGuid();
        DiagramId = diagramId;
        UserId = userId;
        Permission = permission;
    }

    private Share() { }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DiagramId { get; private set; }
    public string UserId { get; init; } = string.Empty;
    public SharePermission Permission { get; init; }


}
