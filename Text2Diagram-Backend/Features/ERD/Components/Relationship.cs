namespace Text2Diagram_Backend.Features.ERD.Components;

public class Relationship
{
    public string SourceEntityName { get; set; } = string.Empty;
    public string DestinationEntityName { get; set; } = string.Empty;
    public RelationshipType SourceRelationshipType { get; set; }
    public RelationshipType DestinationRelationshipType { get; set; }
    public string Description { get; set; } = string.Empty;
}
