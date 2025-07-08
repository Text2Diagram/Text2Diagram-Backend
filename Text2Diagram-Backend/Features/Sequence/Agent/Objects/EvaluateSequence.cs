namespace Text2Diagram_Backend.Features.Sequence.Agent.Objects
{
	public class EvaluateResponseDto
	{
		public bool IsAccurate { get; set; }
		public List<string> MissingElements { get; set; } = new List<string>();
		public List<string> IncorrectElements { get; set; } = new List<string>();
		public List<string> Suggestions { get; set; } = new List<string>();
		public string Commentary { get; set; } = string.Empty;
	}
}
