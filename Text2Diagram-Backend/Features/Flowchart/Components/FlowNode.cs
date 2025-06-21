namespace Text2Diagram_Backend.Features.Flowchart.Components;

public class FlowNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public NodeType Type { get; set; }
}
