using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.ERD.Agents
{
    public static class EvaluateERDiagram
    {
        public static string PromptEvaluateERDiagram(string input, string mermaidCode)
        {
            return @"
You are an expert software architecture AI specializing in validating Entity-Relationship Diagrams (ERDs) against user requirements.
" + Prompts.LanguageRules + @"
            ---
### TASK:
Your job is to carefully analyze whether the **ER diagram (written in Mermaid syntax)** truly and **logically reflects the user’s intent** as described in the input below.
You must evaluate whether the diagram:
1. Accurately represents the core concepts, entities, and relationships described in the input.
2. Includes all **important entities and associations** that are either explicitly mentioned or clearly implied by the business context.
3. Properly reflects how entities relate to each other — including **direction, multiplicity**, and **meaning**.
4. Makes logical sense based on what the user is trying to model (e.g., no obviously incorrect or missing links).
5. Avoids unnecessary or misleading entities/relationships that are not present in or contradict the input.
6. Can reasonably support the system being described, from a data structure and use-case perspective.
---
### OUTPUT:
Provide your structured evaluation in the following format:
```json
{
  ""IsAccurate"": true | false, // true if the diagram accurately reflects the input, false otherwise
  ""MissingElements"": [ ""List any missing entities or relationships that should have been included"" ],
  ""IncorrectElements"": [ ""List any elements that don't logically belong, or that are incorrectly drawn"" ],
  ""Suggestions"": [ ""What could be added, removed, or changed to better align the diagram with the requirement?"" ],
  ""Commentary"": ""Brief summary (2–5 sentences) of how well the diagram aligns with the input.""
}

INPUT DESCRIPTION (User Requirement):" +
input + @"

ER DIAGRAM (Mermaid Syntax)" +
mermaidCode;
        }
    }
}
