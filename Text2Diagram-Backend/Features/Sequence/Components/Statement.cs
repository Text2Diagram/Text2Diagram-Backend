namespace Text2Diagram_Backend.Features.Sequence.Components;
public class Statement : SequenceElement
{
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ArrowType { get; set; } = string.Empty; // "-->", "->>", "x->>", v.v.
}
