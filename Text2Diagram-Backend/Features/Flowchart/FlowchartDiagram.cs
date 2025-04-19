namespace Text2Diagram_Backend.Features.Flowchart;

public record FlowchartDiagram(
    List<FlowNode> Nodes,
    List<FlowEdge> Edges,
    List<Subflow> Subflows
);
