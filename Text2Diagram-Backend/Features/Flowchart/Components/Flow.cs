using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

public record Flow(
    string Name,
    FlowType FlowType,
    List<FlowNode> Nodes,
    List<FlowEdge> Edges
);