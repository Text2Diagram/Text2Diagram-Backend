
namespace Text2Diagram_Backend.Features.ERD.Components
{
	public record ERDiagram
	(
		List<Entity> entities,
		List<Relationship> relationships
	);
}
