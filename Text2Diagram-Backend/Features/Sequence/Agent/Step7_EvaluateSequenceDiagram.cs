namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
    public static class Step7_EvaluateSequenceDiagram
    {
        public static string PromtEvaluateSequenceDiagram(string input, string mermaidCode)
        {
            return @"
You are an expert software architecture AI specializing in validating sequence diagrams against user requirements.
---
### TASK:
Your job is to **analyze the correctness, completeness, and logical consistency** of a sequence diagram written in Mermaid syntax, based on a user-provided functional description (input).
You must evaluate whether the diagram:
1. **Faithfully represents** all key actors, messages, and flows from the input.
2. **Properly uses constructs** like `alt`, `loop`, `par`, `critical`, etc., if they are implied or explicitly described in the input.
3. **Does not include invalid or unnecessary steps** that are absent from the user input.
4. **Maintains correct interaction order** (i.e., chronological flow and causality).
5. Highlights **exceptions or conditions**, if such are present in the user input.
6. Uses correct **participants**, **arrows**, and **messages** based on the described system behavior.
---
### OUTPUT:
Provide your structured evaluation in the following format:

```json
{
  ""IsAccurate"": true | false, // true if the diagram accurately reflects the input, false otherwise
  ""MissingElements"": [ ""list any actors, messages, or conditions missing"" ],
  ""IncorrectElements"": [ ""list any wrongly represented or extra elements"" ],
  ""Suggestions"": [ ""suggest corrections or improvements to the sequence diagram"" ],
  ""Commentary"": ""a brief explanation (2–5 sentences) summarizing your evaluation""
}

INPUT DESCRIPTION:" +
input + @"

SEQUENCE DIAGRAM (Mermaid Syntax):" +
mermaidCode;
        }
    }
}
