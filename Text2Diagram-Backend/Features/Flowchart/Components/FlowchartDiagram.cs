namespace Text2Diagram_Backend.Features.Flowchart.Components;

public record FlowchartDiagram(
    Flow BasicFlow,
    List<Flow> SubFlows
);
