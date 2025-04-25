namespace Text2Diagram_Backend.Data.Models
{
	public class ProjectVM
	{
		public Guid Id { get; init; }
		public Guid WorkspaceId { set; get; }
		public Guid DiagramId { get; private set; }
		public string Data { get; init; } = string.Empty;
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; } = DateTime.Now;
		public DateTime UpdatedAt { get; init; } = DateTime.Now;
	}
}
