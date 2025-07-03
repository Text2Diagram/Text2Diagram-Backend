
namespace Text2Diagram_Backend.Features.ERD.Components;

public class ERDiagram
{
	public List<Entity> Entites { get; set; } = new List<Entity>();
	public List<Relationship> Relationships { get; set; } = new List<Relationship>();
}
