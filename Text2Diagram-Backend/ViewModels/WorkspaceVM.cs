namespace Text2Diagram_Backend.ViewModels
{
	public class WorkspaceVM
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string OwnerId { get; set; } = string.Empty;
	}
}
