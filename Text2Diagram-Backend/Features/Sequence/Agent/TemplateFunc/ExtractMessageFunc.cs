using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;

namespace Text2Diagram_Backend.Features.Sequence.NewWay.TempFunc
{
	public static class ExtractMessageFunc
	{
		public static string ExtractMessage(string stepText, StepParticipantDto participant)
		{
			if (participant == null) return stepText;

			// Remove subject to get message: Ex: "User clicks the button" => "clicks the button"
			if (!string.IsNullOrEmpty(participant.Sender) && stepText.StartsWith(participant.Sender, StringComparison.OrdinalIgnoreCase))
			{
				return stepText.Substring(participant.Sender.Length).TrimStart(new[] { ' ', ':' });
			}

			return stepText;
		}

		public static string ExtractJsonFromText(string textContent)
		{
			var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
			if (codeFenceMatch.Success)
				return codeFenceMatch.Groups[1].Value.Trim();

			var rawJsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}", RegexOptions.Singleline);
			return rawJsonMatch.Success ? rawJsonMatch.Value.Trim() : "";
		}

	}
}
