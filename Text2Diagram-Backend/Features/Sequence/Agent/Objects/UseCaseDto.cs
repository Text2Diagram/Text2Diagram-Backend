namespace Text2Diagram_Backend.Features.Sequence.NewWay.Objects
{
	public class UseCaseDto
	{
		public string UseCase { get; set; }
		//public string Description { get; set; }
		//public string Actor { get; set; }
		//public string Preconditions { get; set; }
		//public string Postconditions { get; set; }
		public List<string> BasicFlow { get; set; }
		public List<string> AlternativeFlows { get; set; }
		public List<string> ExceptionFlows { get; set; }
	}

}
