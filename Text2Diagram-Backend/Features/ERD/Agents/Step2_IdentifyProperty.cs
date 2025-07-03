namespace Text2Diagram_Backend.Features.ERD.Agents
{
    public static class Step2_IdentifyProperty
    {
        public static string PromtIdentifyProperty(string input, string entities)
        {
            return @"
You are a professional software architect assisting in building a structured Entity Relationship Diagram (ERD) from software requirements.
In the previous step, the following Entities were identified:
### Entities:"
+ entities + @"
Now, based on the input text below, your task is to analyze and extract a list of **Properties (fields/attributes)** for each of these entities.
" + input + @"
---
🧠 **Rules for Property Extraction**:
1. Identify all meaningful attributes that belong to each Entity based on the context.
2. Use domain knowledge to infer fields that are likely required even if they are implied (e.g., an `ENROLLMENT` likely has `student_id`, `course_id`, etc.).
3. Each Property should follow this structure:
   - `""Type""`: a simple valid data type such as `""string""`, `""int""`, `""float""`, `""bool""`, or `""string[]""`.
   - `""Name""`: a concise field name, using **camelCase** or lowercase with underscores. No spaces.
   - `""Role""`: must be one of:
     - `""PK""` for Primary Key
     - `""FK""` for Foreign Key
     - `""""` (empty string) if neither
   - `""Description""`: a brief explanation of what the property represents.
---
📏 **Output Format**:
Return a list of objects where:
- Each object represents one Entity.
- Each entity has a `Name` and a list of `Properties`.
### Example Output:
```json
[
  {
    ""Name"": ""STUDENT"",
    ""Properties"": [
      { ""Type"": ""string"", ""Name"": ""id"", ""Role"": ""PK"", ""Description"": ""student identifier"" },
      { ""Type"": ""string"", ""Name"": ""name"", ""Role"": """", ""Description"": ""student name"" },
      { ""Type"": ""string"", ""Name"": ""email"", ""Role"": """", ""Description"": ""student email address"" }
    ]
  },
  {
    ""Name"": ""COURSE"",
    ""Properties"": [
      { ""Type"": ""string"", ""Name"": ""id"", ""Role"": ""PK"", ""Description"": ""course identifier"" },
      { ""Type"": ""string"", ""Name"": ""title"", ""Role"": """", ""Description"": ""course title"" }
    ]
  }
]
";
        }
    }
}
