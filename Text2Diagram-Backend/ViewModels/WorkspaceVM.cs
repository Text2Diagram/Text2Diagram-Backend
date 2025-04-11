namespace Text2Diagram_Backend.ViewModels
{
	public class WorkspaceVM
	{
		public Guid Id { get; init; }
		public string Name { get; private set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; set; }
		public string OwnerId { get; set; } = string.Empty;
	}
}
