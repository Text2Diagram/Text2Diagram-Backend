using System.Text.RegularExpressions;

namespace Text2Diagram_Backend.Features.Helper
{
	public static class ExtractJsonFromTextHelper
	{
		public  static string ExtractJsonFromText(string textContent)
		{
			// Ưu tiên tìm trong code block có đánh dấu ```json
			var codeFenceMatch = Regex.Match(textContent, @"```(?:json)?\s*([\s\S]+?)\s*```", RegexOptions.Singleline);
			if (codeFenceMatch.Success)
			{
				return codeFenceMatch.Groups[1].Value.Trim();
			}

			// Nếu không có code fence, tìm JSON array hoặc object
			var arrayMatch = Regex.Match(textContent, @"\[\s*{[\s\S]*?}\s*\]", RegexOptions.Singleline);
			if (arrayMatch.Success)
			{
				return arrayMatch.Value.Trim();
			}

			var objectMatch = Regex.Match(textContent, @"{[\s\S]*}", RegexOptions.Singleline);
			if (objectMatch.Success)
			{
				return objectMatch.Value.Trim();
			}

			return "";
		}
	}
}
