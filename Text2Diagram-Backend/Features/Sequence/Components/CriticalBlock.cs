namespace Text2Diagram_Backend.Features.Sequence.Components;
public class CriticalBlock : SequenceElement
{
	public string Title { get; set; } // e.g. "Establish a connection to the DB"
	public List<SequenceElement> Body { get; set; } = new();
	public List<OptionBlock> Options { get; set; } = new();
}

public class OptionBlock : SequenceElement
{
	public string Condition { get; set; } // e.g. "Network timeout"
	public List<SequenceElement> Body { get; set; } = new();
}
