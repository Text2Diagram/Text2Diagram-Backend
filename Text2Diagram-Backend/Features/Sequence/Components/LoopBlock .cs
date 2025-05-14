namespace Text2Diagram_Backend.Features.Sequence.Components;
public class LoopBlock : SequenceElement
{
	public string Title { get; set; } // Ví dụ: "Every minute"
	public List<SequenceElement> Body { get; set; } = new();
}
