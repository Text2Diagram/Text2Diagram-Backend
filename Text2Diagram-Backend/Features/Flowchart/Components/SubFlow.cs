using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

public record SubFlow(
    string Name,
    List<FlowNode> Nodes,
    List<FlowEdge> Edges
);