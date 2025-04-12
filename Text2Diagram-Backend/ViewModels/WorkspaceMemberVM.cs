using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.ViewModels
{
	public class WorkspaceMemberVM
	{
		public Guid Id { get; set; }
		public Guid WorkspaceId { get; set; }
		public string UserId { get; init; } = string.Empty;
		public WorkspaceRole Role { get; init; }
	}
}
