namespace Text2Diagram_Backend.Features.ERD.Agents
{
	public static class Step3_IdentifyRelation
	{
		public static string PromtIdentifyRelation(string input, string entity)
		{
			return @"
You are a software modeling assistant. Your task is to analyze the user requirements below and identify the relationships between entities in the system, including how many instances of each entity can be related to the other.

---

📌 Definitions and Rules:

- You are given a list of identified entities in the system.
- For each pair of related entities, describe the **relationship directionally**: from the `SourceEntityName` to the `DestinationEntityName`.

- Each relationship must include:
  - `SourceEntityName`: the entity **initiating** the relationship.
  - `DestinationEntityName`: the **target** entity.
  - `SourceRelationshipType`: the number of `DestinationEntity` instances that a **single `SourceEntity`** can relate to.
  - `DestinationRelationshipType`: the number of `SourceEntity` instances that a **single `DestinationEntity`** can relate to.
  - `Description`: a clear and short description of the relationship in natural language.

- Valid values for relationship types:
  - `""ZeroOrOne""` — zero or one instance
  - `""ExactlyOne""` — exactly one instance
  - `""ZeroOrMore""` — zero or many instances
  - `""OneOrMore""` — one or many instances

- Do NOT include relationships unless clearly stated or logically inferred.
- Only use entity names from the list provided.
- Avoid duplicate or reversed versions of the same relationship.

---

🧱 Entity List:
"+ entity + @"
---
📥 User Requirement Input:" + input + @"
---
✅ Return Format (JSON only):
Return the list of relationships using the format below. Output must be a valid JSON array.
```json
[
  {
    ""SourceEntityName"": ""CHILD"",
    ""DestinationEntityName"": ""MOM"",
    ""SourceRelationshipType"": ""ExactlyOne"", // one child has exactly one mom
    ""DestinationRelationshipType"": ""ZeroOrMore"", // one mom can have zero or many children
    ""Description"": ""has""
  }
]
";
		}
	}
}
