using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json;

namespace Text2Diagram_Backend.Data.Models
{
	public class Project
	{
		public Guid Id { get; init; }
		public Guid WorkspaceId { set; get; }
<<<<<<< HEAD
		[Column(TypeName = "jsonb")]
		public JsonDocument Data { get; init; }
=======

		[Column(TypeName = "jsonb")]
		public object Data { get; set; }
>>>>>>> 6d93e69383f3e1a323143e7ca054ffbb65b00141
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }

		private Project() { }

<<<<<<< HEAD
		public Project(Guid workspaceId, JsonDocument data, string name, string thumbnail)
=======
		public Project(Guid workspaceId, object data, string name, string thumbnail)
>>>>>>> 6d93e69383f3e1a323143e7ca054ffbb65b00141
		{
			Id = Guid.NewGuid();
			WorkspaceId = workspaceId;
			Data = data;
			Thumbnail = thumbnail;
			Name = name;
		}
	}
}
