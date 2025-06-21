namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
	public static class Step3_IdentifyParticipant
	{
		public static string IdentifyParticipants(string flowUseCases)
		{
			return @"
				You are a senior software engineer helping to create a sequence diagram.

				You are given a combinedFlow of a use case, which is a list of steps that include normal, alternative, and exception flows.

				Your task is to identify the two participants involved in each step:
				- The **sender** (who initiates or performs the action)
				- The **receiver** (who is affected or responds to the action)

				Rules:
				- Only include a receiver if it's **clearly stated or implied** in the step.
				- If an action involves only one actor (e.g., ""The user opens the app.""), set `receiver` to an empty string `""""`.
				- Use participant names explicitly mentioned in the sentence (e.g., User, System, Store).
				- For steps like ""Alternative: ..."" or ""Exception: ..."", still analyze the action and extract participants accordingly.

				Each step should be mapped in this JSON format:
				{
				  ""step"": ""<original step text>"",
				  ""sender"": ""<participant who performs the action>"",
				  ""receiver"": ""<participant who receives or is affected, or empty string if none>""
				}

				Return the result as an **ordered JSON list** of these objects.

				Here is the combined flow to process:
			" + flowUseCases;
		}
	}
}
