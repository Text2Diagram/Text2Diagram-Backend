namespace Text2Diagram_Backend.Features.Flowchart.Components;

public class FlowEdge
{
    public string SourceId { get; set; }
    public string TargetId { get; set; }
    public EdgeType Type { get; private set; }
    public string? Label { get; private set; }

    public FlowEdge(string sourceId, string targetId, EdgeType type, string? label)
    {
        SourceId = sourceId;
        TargetId = targetId;
        Type = type;
        Label = label;
    }
}