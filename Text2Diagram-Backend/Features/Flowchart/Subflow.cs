namespace Text2Diagram_Backend.Features.Flowchart;

public record Subflow(
    string Name,
    List<FlowNode> Nodes,
    List<FlowEdge> Edges
);