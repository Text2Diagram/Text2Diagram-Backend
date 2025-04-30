using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json;

namespace Text2Diagram_Backend.Data.Models
{
	public class Project
	{
		public Guid Id { get; init; }
		public Guid WorkspaceId { set; get; }
		[Column(TypeName = "jsonb")]
		public JsonDocument Data { get; init; }
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; set; }

		private Project() { }

		public Project(Guid workspaceId, JsonDocument data, string name, string thumbnail)
		{
			Id = Guid.NewGuid();
			WorkspaceId = workspaceId;
			Data = data;
			Thumbnail = thumbnail;
			Name = name;
		}
	}
}
