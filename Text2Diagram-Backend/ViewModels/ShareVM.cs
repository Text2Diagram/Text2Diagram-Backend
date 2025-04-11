using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.ViewModels
{
	public class ShareVM
	{
		public Guid Id { get; init; } = Guid.NewGuid();
		public Guid DiagramId { get; private set; }
		public string UserId { get; init; } = string.Empty;
		public SharePermission Permission { get; init; }
	}
}
