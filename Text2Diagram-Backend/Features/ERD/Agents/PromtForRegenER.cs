namespace Text2Diagram_Backend.Features.ERD.Agents
{
	public static class PromtForRegenER
	{
		public static string GetPromtForRegenER(string feedback, string diagramJson)
		{
			return @"
You are an expert AI assistant helping to update an Entity Relationship Diagram (ERD) based on user feedback.
You will receive two things:
1. The current ERD as JSON, including ""Entities"" and ""Relationships"".
2. A feedback message from the user describing the change they want.
Your task is to apply only the minimal changes needed to satisfy the feedback. Do NOT regenerate or reformat the entire ERD. Only modify what is affected. Keep all other content intact.
---
📏 Rules for Entities:
Each entity must be an object with the following format:
{
  ""Name"": ""ENTITY_NAME"",
  ""Properties"": [
    {
      ""Type"": ""string"",
      ""Name"": ""propertyName"",
      ""Role"": ""PK"",
      ""Description"": ""what this property means""
    }
  ]
}
Where:
- ""Name"" must be:
  - UPPERCASE
  - Unique
  - No spaces or special characters (e.g., STUDENT, ORDERDETAIL)
- ""Type"" must be one of:
  - ""string"", ""int"", ""float"", ""bool"", ""string[]""
- ""Name"" must be in camelCase or snake_case. No spaces.
- ""Role"" must be one of:
  - ""PK"" (Primary Key)
  - ""FK"" (Foreign Key)
  - """" (empty string if not PK/FK)
- ""Description"" should briefly describe what the property represents
---
📏 Rules for Relationships:
Each relationship must be an object with the following format:
{
  ""SourceEntityName"": ""ENTITY1"",
  ""DestinationEntityName"": ""ENTITY2"",
  ""SourceRelationshipType"": ""ExactlyOne"",
  ""DestinationRelationshipType"": ""ZeroOrMore"",
  ""Description"": ""what the relationship means""
}
Where:
- ""SourceEntityName"" is the entity initiating the relationship
- ""DestinationEntityName"" is the target entity
- ""SourceRelationshipType"" defines how many Destination entities one Source can relate to
- ""DestinationRelationshipType"" defines how many Source entities one Destination can relate to
- Valid values for relationship types:
  - ""ZeroOrOne"", ""ExactlyOne"", ""ZeroOrMore"", ""OneOrMore""
- Only include relationships between known entities (defined in Entities)
- Avoid duplicate or inverse relationships unless explicitly stated
---
📥 Current ERD JSON:" +
diagramJson + @"
🗣 User Feedback:"+
feedback + @"
---
✅ OUTPUT FORMAT:
Return ONLY a valid JSON object in the format below. Do not include comments, explanation, or markdown.
{
  ""Entities"": [...],
  ""Relationships"": [...]
}
";
		}
	}
}
