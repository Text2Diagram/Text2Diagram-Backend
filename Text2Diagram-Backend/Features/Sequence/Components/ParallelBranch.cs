namespace Text2Diagram_Backend.Features.Sequence.Components;

public class ParallelBranch : SequenceElement
{
    public string Title { get; set; } = string.Empty; // optional
    public List<SequenceElement> Body { get; set; } = new();
}
