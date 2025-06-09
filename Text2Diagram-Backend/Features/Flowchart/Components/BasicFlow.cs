namespace Text2Diagram_Backend.Features.Flowchart.Components;

public record BasicFlow
(
    List<FlowNode> Nodes,
    List<FlowEdge> Edges
);