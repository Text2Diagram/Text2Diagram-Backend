namespace Text2Diagram_Backend.Features.Sequence.Components;

public class AltBranch : SequenceElement
{
    public string Condition { get; set; } = string.Empty; // "is sick", "is well"
    public List<SequenceElement> Body { get; set; } = new();
}