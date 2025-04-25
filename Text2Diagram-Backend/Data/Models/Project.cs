using System.Data;

namespace Text2Diagram_Backend.Data.Models
{
	public class Project
	{
		public Guid Id { get; init; } = Guid.NewGuid();
		public Guid WorkspaceId { set; get; }
		public string Data { get; init; } = string.Empty;
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; set; }

		private Project() { }

		public Project(Guid workspaceId, string data, string name, string thumbnail)
		{
			Id = Guid.NewGuid();
			WorkspaceId = workspaceId;
			Data = data;
			Thumbnail = thumbnail;
			Name = name;
		}
	}
}
