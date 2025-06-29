using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;
using Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc;

namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
	public static class Step5_CombineLLMResult
	{
		public static List<StepFinalDto> CombineLLMResults(
		UseCaseInputDto useCaseInput,
		List<StepParticipantDto> participants,
		List<StepControlTypeDto> controlTypes)
		{
			var finalSteps = new List<StepFinalDto>();

			foreach (var stepText in useCaseInput.CombinedFlow)
			{
				var participant = participants.FirstOrDefault(p =>
					string.Equals(p.Step.Trim(), stepText.Trim(), StringComparison.OrdinalIgnoreCase));

				var control = controlTypes.FirstOrDefault(c =>
					string.Equals(c.Step.Trim(), stepText.Trim(), StringComparison.OrdinalIgnoreCase));

				finalSteps.Add(new StepFinalDto
				{
					Sender = participant?.Sender ?? "Unknown",
					Receiver = participant?.Receiver ?? "Unknown",
					Message = ExtractMessageFunc.ExtractMessage(stepText, participant),
					ActionType = control?.Type ?? "normal",
					Condition = control?.Condition ?? ""
				});
				}

			return finalSteps;
		}

	}
}
