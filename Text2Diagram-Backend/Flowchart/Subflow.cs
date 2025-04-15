namespace Text2Diagram_Backend.Flowchart;

public record Subflow(
    string Name,
    List<FlowNode> Nodes,
    List<FlowEdge> Edges
);