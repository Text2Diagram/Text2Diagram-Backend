using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
    public static class Step3_IdentifyParticipant
    {
        public static string IdentifyParticipants(string flowUseCases)
        {
            return @"
				You are a senior software engineer with deep knowledge of sequence diagrams.

				You are given a list of steps called combinedFlow from a software use case specification. This list includes basic, alternative, and exception flows.
" + Prompts.LanguageRules + @"
				Your task is to identify the two participants involved in each step:
				- ""sender"": the actor or system that initiates or performs the action
				- ""receiver"": the actor, system, or component that receives or reacts to the action

				For each step, analyze the action carefully and assign appropriate participants.
				If the step involves an internal system interaction, identify the correct internal service or component (e.g., PaymentService, InventoryService, Database).
				Avoid defaulting everything to just ""User"" and ""System"" unless it's truly correct.
				Do not guess beyond the context, but be as specific as logically possible.

				Return the result as an **ordered list of JSON objects** in the following format:

				```json
				{
				  ""step"": ""<original step text>"",
				  ""sender"": ""<name of sender participant>"",
				  ""receiver"": ""<name of receiver participant>""
				}

				Here is the combined flow to process:
			" + flowUseCases;
        }
    }
}
