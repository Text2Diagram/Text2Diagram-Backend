namespace Text2Diagram_Backend.Flowchart;

public record FlowchartDiagram(
    List<FlowNode> Nodes,
    List<FlowEdge> Edges,
    List<Subflow> Subflows
);
