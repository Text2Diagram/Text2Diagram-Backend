using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Text2Diagram_Backend.Services
{
	public class ObjectToJsonSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			if (context.Type == typeof(object))
			{
				schema.Type = "object";
				schema.Properties = new Dictionary<string, OpenApiSchema>();
				schema.AdditionalPropertiesAllowed = true;
			}
		}
	}
}
