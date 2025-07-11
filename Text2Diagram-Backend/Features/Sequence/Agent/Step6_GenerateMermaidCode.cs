using System.Text.Json;
using Text2Diagram_Backend.Features.Flowchart;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;

namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
    public static class Step6_GenerateMermaidCode
    {
        public static string GenerateMermaidCode(List<StepFinalDto> allSteps)
        {
            //var batch = SplitFlowIntoBatches(allSteps);
            return @"
You are an expert AI agent that converts structured software steps into Mermaid-compatible sequence diagram code.
" + Prompts.LanguageRules + @"
---
### INPUT FORMAT
You are given a list of JSON objects, each describing one step in a use case interaction flow.  
Each object has:
- `Step`: Full text of the flow step
- `Sender`: Who initiates the action
- `Receiver`: Who receives or processes the action
- `Message`: Description of the interaction
- `ActionType`: One of: `""normal""`, `""alt""`, `""loop""`, `""par""`, `""critical""`
- `Condition`: Optional string, used for alt/loop/critical control blocks

📌 Here is an example input:
```json
[
  {
    ""Step"": ""User clicks login"",
    ""Sender"": ""User"",
    ""Receiver"": ""UI"",
    ""Message"": ""click login"",
    ""ActionType"": ""normal"",
    ""Condition"": """"
  },
  {
    ""Step"": ""UI checks credentials"",
    ""Sender"": ""UI"",
    ""Receiver"": ""AuthService"",
    ""Message"": ""validate credentials"",
    ""ActionType"": ""alt"",
    ""Condition"": ""If credentials are valid""
  },
  {
    ""Step"": ""Show success"",
    ""Sender"": ""AuthService"",
    ""Receiver"": ""User"",
    ""Message"": ""Login success"",
    ""ActionType"": ""normal"",
    ""Condition"": """"
  }
]

### OUTPUT FORMAT:
sequenceDiagram
    participant User
    participant UI
    participant AuthService

    User->>UI: click login
    UI->>AuthService: validate credentials
    alt If credentials are valid
        AuthService-->>User: Login success
    end
---------
Here is the input:
" + JsonSerializer.Serialize(allSteps);
        }

        public static List<List<StepFinalDto>> SplitFlowIntoBatches(List<StepFinalDto> allSteps, int batchSize = 10)
        {
            return allSteps
                .Select((step, index) => new { step, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.step).ToList())
                .ToList();
        }
    }
}
