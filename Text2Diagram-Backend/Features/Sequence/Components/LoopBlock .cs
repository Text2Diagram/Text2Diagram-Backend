namespace Text2Diagram_Backend.Features.Sequence.Components;
public class LoopBlock : SequenceElement
{
    public string Title { get; set; } = string.Empty; // Ví dụ: "Every minute"
    public List<SequenceElement> Body { get; set; } = new();
}
