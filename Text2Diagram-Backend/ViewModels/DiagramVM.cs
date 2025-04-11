using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.ViewModels
{
	public class DiagramVM
	{
		public Guid Id { get; private set; } = Guid.NewGuid();
		public string Title { get; private set; } = string.Empty;
		public string? Description { get; set; }
		public string DiagramData { get; set; } = string.Empty;
		public DiagramType DiagramType { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; set; }
		public bool IsPublic { get; set; } = false;
		public string UserId { get; init; } = string.Empty;
	}
}
