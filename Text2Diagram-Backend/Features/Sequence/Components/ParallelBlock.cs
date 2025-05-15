namespace Text2Diagram_Backend.Features.Sequence.Components;

public class ParallelBlock : SequenceElement
{
    public string Title { get; set; } = string.Empty; // optional, e.g., "Alice to Bob"
    public List<ParallelBranch> Branches { get; set; } = new();
}
