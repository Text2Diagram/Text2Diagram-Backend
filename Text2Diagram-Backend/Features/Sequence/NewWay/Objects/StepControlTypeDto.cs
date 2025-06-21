namespace Text2Diagram_Backend.Features.Sequence.NewWay.Objects
{
	public class StepControlTypeDto
	{
		public string Step { get; set; }
		public string Type { get; set; } // "normal", "alt", etc.
		public string Condition { get; set; }
	}
}
