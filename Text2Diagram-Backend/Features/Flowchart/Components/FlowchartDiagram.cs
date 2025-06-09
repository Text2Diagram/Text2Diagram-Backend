namespace Text2Diagram_Backend.Features.Flowchart.Components;

public record FlowchartDiagram(
    BasicFlow BasicFlow,
    List<SubFlow> SubFlows
);
