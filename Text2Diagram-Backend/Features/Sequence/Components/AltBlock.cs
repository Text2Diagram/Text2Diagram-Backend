namespace Text2Diagram_Backend.Features.Sequence.Components;

public class AltBlock : SequenceElement
{
    public List<AltBranch> Branches { get; set; } = new();
}
