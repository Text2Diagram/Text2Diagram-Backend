namespace Text2Diagram_Backend.Features.Flowchart;

public class FlowEdge
{
    public string Id { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Label { get; set; }
    public EdgeType Type { get; set; }
}