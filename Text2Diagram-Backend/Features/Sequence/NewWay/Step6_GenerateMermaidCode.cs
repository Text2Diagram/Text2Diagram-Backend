namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
	public static class Step6_GenerateMermaidCode
	{
		public static string GenerateMermaidCode(string flow)
		{
			return @"
				You are given a list of structured steps from a use case flow.

				Each step includes sender, receiver, message, actionType, and optional condition.

				Please convert this into Mermaid sequence diagram code.

				- For `actionType = normal`, use standard message lines.
				- For `actionType = alt`, `else`, `loop`, `par`, or `critical`, wrap steps accordingly using Mermaid control blocks.
				- Ensure nesting and indentation is correct.

				Return only the Mermaid diagram content.

				Here is the list of steps:
			" + flow;
		}
	}
}
