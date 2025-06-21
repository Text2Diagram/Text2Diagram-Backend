namespace Text2Diagram_Backend.Features.Sequence.Components;

public class OptionBlock : SequenceElement
{
    public string Condition { get; set; } = string.Empty; // e.g. "Network timeout"
    public List<SequenceElement> Body { get; set; } = new();
}
