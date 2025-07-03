namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
	public static class Step7_Final
	{
		public static string PromtFinal(string json)
		{
			return @"
You are a Mermaid Sequence Diagram Generator.

You are given a list of flow steps with the following structure per item:
- Sender: who initiates the action
- Receiver: who receives the action
- Message: the message content or function call
- ActionType: the interaction type (normal, alt, loop, par, critical)
- Condition: optional condition used for alt/loop/critical blocks

### Your task:
1. For each item:
   - If ActionType is ""normal"", generate a Statement:
     {
       ""Sender"": ""..."",
       ""Receiver"": ""..."",
       ""Message"": ""..."",
       ""ArrowType"": ""->>""
     }
   - If ActionType is ""alt"" or ""else"", group into AltBlock with Condition
   - If ActionType is ""loop"", group into LoopBlock using Condition as Title
   - If ActionType is ""par"", group into ParallelBlock
   - If ActionType is ""critical"", group into CriticalBlock

2. Output a valid JSON object with the structure:
```json
{
  ""Elements"": [
    // List of SequenceElements (Statement, AltBlock, LoopBlock, etc.)
  ]
}

";
		}
	}
}
