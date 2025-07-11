using Text2Diagram_Backend.Features.Flowchart;
namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
    public static class Step4_IdentifyCondition
    {
        public static string IdentifyCondition(string flow)
        {
            return @"
			You are a senior software engineer helping to generate a sequence diagram.

			You are given a combinedFlow of a use case. Each step describes an action.
" + Prompts.LanguageRules + @"
			Your task is to analyze each step and determine whether it represents any of the following control expressions in a sequence diagram:
			- `alt` (conditional branch)
			- `else` (alternative branch)
			- `loop` (repeated steps)
			- `par` (parallel execution)
			- `critical` (synchronized region)
			- `normal` (default sequential step)

			For each step, return the following information:

			{
			  ""step"": ""<original step text>"",
			  ""type"": ""<control expression: normal | alt | else | loop | par | critical>"",
			  ""condition"": ""<optional condition or trigger text>""
			}

			Make reasonable assumptions based on keywords such as ""if"", ""can"", ""when"", ""repeat"", ""parallel"", ""simultaneously"", ""only if"", etc.

			Respond with an array in the same order as the input.

			Here is the combinedFlow:
			" + flow;
        }
    }
}
