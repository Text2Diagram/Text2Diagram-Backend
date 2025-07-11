using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.ERD.Agents
{
    public static class Step1_IdentifyEntity
    {
        public static string GetPromtIdentifyEntity(string input)
        {
            return @"
You are an expert in analyzing software requirement specifications and extracting core data models for building Entity Relationship Diagrams (ERD). 
Your task is to read the input below and identify all the relevant **Entities** described or implied. 
" + Prompts.LanguageRules + @"
---
🔍 **Definition of an Entity**:
An Entity represents a core object or concept in the domain that has a distinct identity and typically corresponds to a table in a relational database. 
It usually:
- Has multiple attributes or fields describing it.
- Is involved in relationships with other entities (e.g., one-to-many, many-to-many).
- Appears repeatedly in the domain as a central object (e.g., STUDENT, COURSE, ORDER).
Think of entities as **“things or concepts that the system needs to keep information about”**.
---
🧠 **Your Rules**:
- Return only real, meaningful entities. Avoid vague terms or actions like “Login” or “Submit Form”.
- Ignore system operations or UI components unless they represent persistent domain objects.
- Use domain understanding to infer implicit entities if needed.
---
📏 **Formatting Rules**:
- Entity names must be:
  - UPPERCASE
  - Unique
  - No spaces or special characters (e.g., STUDENT, ORDERDETAIL, COURSEENROLLMENT)
- Return your answer in the following format (a JSON array of strings):
```json
[""STUDENT"", ""COURSE"", ""ENROLLMENT""]

Here is the input to analyze:
" + input;
        }
    }
}
