using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.ViewModels
{
	public class ShareVM
	{
		public Guid Id { get; set; }
		public Guid DiagramId { get; set; }
		public string UserId { get; init; } = string.Empty;
		public SharePermission Permission { get; init; }
	}
}
