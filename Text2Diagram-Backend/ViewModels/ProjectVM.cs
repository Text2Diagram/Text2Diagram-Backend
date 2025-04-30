using System.Text.Json;

namespace Text2Diagram_Backend.Data.Models
{
	public class ProjectVM
	{
		public Guid Id { get; set; }
		public Guid WorkspaceId { set; get; }
		public JsonDocument Data { get; set; }
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; set; }
	}
}
