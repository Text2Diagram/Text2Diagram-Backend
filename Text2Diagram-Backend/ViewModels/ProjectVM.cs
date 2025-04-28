using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace Text2Diagram_Backend.Data.Models
{
	public class ProjectVM
	{
		public Guid WorkspaceId { set; get; }
		//[SwaggerSchema("object")]
		public object Data { get; set; }
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime? UpdatedAt { get; set; }
	}
}
