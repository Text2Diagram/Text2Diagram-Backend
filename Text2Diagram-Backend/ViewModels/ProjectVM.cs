<<<<<<< HEAD
﻿using System.Text.Json;
=======
﻿using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
>>>>>>> 6d93e69383f3e1a323143e7ca054ffbb65b00141

namespace Text2Diagram_Backend.Data.Models
{
	public class ProjectVM
	{
		public Guid WorkspaceId { set; get; }
<<<<<<< HEAD
		public JsonDocument Data { get; set; }
=======
		//[SwaggerSchema("object")]
		public object Data { get; set; }
>>>>>>> 6d93e69383f3e1a323143e7ca054ffbb65b00141
		public string Name { get; init; } = string.Empty;
		public string Thumbnail { get; init; } = string.Empty;
		public DateTime? UpdatedAt { get; set; }
	}
}
