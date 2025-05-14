namespace Text2Diagram_Backend.Features.Sequence.Components;
public class Statement : SequenceElement
{
	public string Participant1 { get; set; }
	public string Participant2 { get; set; }
	public string Message { get; set; }
	public string ArrowType { get; set; } // "-->", "->>", "x->>", v.v.
}
