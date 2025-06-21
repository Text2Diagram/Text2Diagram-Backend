
namespace Text2Diagram_Backend.Features.ERD.Components;

public record ERDiagram
(
    List<Entity> Entites,
    List<Relationship> Relationships
);
