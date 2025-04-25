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
		public DateTime CreatedAt { get; init; } = DateTime.Now;
		public DateTime UpdatedAt { get; init; } = DateTime.Now;

		private Project() { }

		public Project(Guid WorkspaceId, string Data, string Name, string Thumbnail, DateTime CreatedAt, DateTime UpdatedAt)
		{
			Id = Guid.NewGuid();
			WorkspaceId = WorkspaceId;
			Data = Data;
			Thumbnail = Thumbnail;
			Name = Name;
			CreatedAt = CreatedAt;
			UpdatedAt = UpdatedAt;
		}
	}
}
