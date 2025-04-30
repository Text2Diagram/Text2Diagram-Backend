namespace Text2Diagram_Backend.Features.ERD.Components
{
	public class Entity
	{
		public string name { get; set; } = string.Empty;
		public List<Property> properties { get; set; }
	}
}
