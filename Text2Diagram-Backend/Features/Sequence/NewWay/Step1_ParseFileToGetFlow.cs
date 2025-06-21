using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.Sequence.NewWay.Objects;

namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
	public static class Step1_ParseFileToGetFlow
	{
		public static string GenPromtInStep1(string input)
		{
			return @"
			You are a software engineer. Extract all use cases from the input text and return a JSON array.

			Each object must include only:
			- useCase: the name of the use case.
			- basicFlow: list of basic flow steps (one step per string).
			- alternativeFlows: list of alternative flow steps (one step per string).
			- exceptionFlows: list of exception flow steps (one step per string).

			Preserve exact sentence content in the steps, including bullet lines like ""- Detail about the step"".

			If the text contains multiple use cases, return a JSON array of objects, one per use case.

			Output example format:

			[
			  {
				""useCase"": ""Example Use Case"",
				""basicFlow"": [
				  ""Step 1."",
				  ""Step 2."",
				  ""- Sub-step A"",
				  ""Step 3.""
				],
				""alternativeFlows"": [
				  ""Alt step 1."",
				  ""- Alt sub-step A""
				],
				""exceptionFlows"": [
				  ""Exception 1."",
				  ""- Exception detail A""
				]
			  }
			]

			Now, process the following input:
			" + input;
		}

		public static List<UseCaseDto> Parse(string input)
		{
			var pattern = @"Use Case:\s*(?<useCase>.*?)\s*"
						+ @"Description:\s*(?<description>.*?)\s*"
						+ @"Actor:\s*(?<actor>.*?)\s*"
						+ @"Preconditions:\s*(?<preconditions>.*?)\s*"
						+ @"Postconditions:\s*(?<postconditions>.*?)\s*"
						+ @"Basic Flow:\s*(?<basicFlow>(?:.|\n)*?)(?=(Alternative Flows:|Exception Flows:|$))"
						+ @"(?:Alternative Flows:\s*(?<alternativeFlows>(?:.|\n)*?))?"
						+ @"(?:Exception Flows:\s*(?<exceptionFlows>(?:.|\n)*))?";

			var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Multiline);
			var matches = regex.Matches(input);
			var result = new List<UseCaseDto>();

			foreach (Match match in matches)
			{
				var dto = new UseCaseDto
				{
					UseCase = match.Groups["useCase"].Value.Trim(),
					BasicFlow = ExtractSteps(match.Groups["basicFlow"].Value),
					AlternativeFlows = ExtractSteps(match.Groups["alternativeFlows"].Success ? match.Groups["alternativeFlows"].Value : ""),
					ExceptionFlows = ExtractSteps(match.Groups["exceptionFlows"].Success ? match.Groups["exceptionFlows"].Value : "")
				};
				result.Add(dto);
			}

			return result;
		}



		private static List<string> ExtractSteps(string section)
		{
			var result = new List<string>();
			if (string.IsNullOrWhiteSpace(section)) return result;

			var lines = section.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			string currentStep = "";

			foreach (var rawLine in lines)
			{
				var line = rawLine.Trim();

				if (Regex.IsMatch(line, @"^\d+\.")) // Bắt đầu bước mới
				{
					if (!string.IsNullOrEmpty(currentStep))
						result.Add(currentStep.Trim());

					currentStep = line;
				}
				else if (line.StartsWith("-")) // dòng phụ đi kèm bước trước
				{
					currentStep += " " + line;
				}
				else
				{
					currentStep += " " + line;
				}
			}

			if (!string.IsNullOrEmpty(currentStep))
				result.Add(currentStep.Trim());

			return result;
		}


	}

}
