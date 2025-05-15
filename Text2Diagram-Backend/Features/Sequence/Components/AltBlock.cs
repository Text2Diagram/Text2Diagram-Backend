namespace Text2Diagram_Backend.Features.Sequence.Components;

public class AltBlock : SequenceElement
{
	public List<AltBranch> Branches { get; set; } = new();
}

public class AltBranch : SequenceElement
{
	public string Condition { get; set; } // "is sick", "is well"
	public List<SequenceElement> Body { get; set; } = new();
}