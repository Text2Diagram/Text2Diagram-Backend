using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.Sequence.Agent
{
    public static class PromtForRegenSequence
    {
        public static string GetPromtForRegenSequence(string feedback, string mermaidCode)
        {
            return @"
You are a smart AI agent that edits Mermaid sequence diagrams based on user feedback.
" + Prompts.LanguageRules + @"
You will receive:
1. An existing sequence diagram (in full Mermaid syntax).
2. A user feedback describing exactly what should be modified.
Your job:
- Apply only the necessary changes to the diagram.
- Do NOT rewrite the entire diagram.
- Update only the affected lines or blocks based on feedback.

📥 Current Mermaid code:" +
mermaidCode + @"
🗣 User Feedback:" +
feedback + @"
Return only the updated diagram wrapped in triple backticks with the mermaid tag.
### Example:
Diagram:
```mermaid
sequenceDiagram
    participant User
    participant UI
    participant System

    User->>UI: Click Checkout
    UI->>System: Add item
";
        }
    }
}
