namespace Text2Diagram_Backend.Features.Sequence.Components;
public class Statement : SequenceElement
{
    public string Participant1 { get; set; } = string.Empty;
    public string Participant2 { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ArrowType { get; set; } = string.Empty; // "-->", "->>", "x->>", v.v.
}
