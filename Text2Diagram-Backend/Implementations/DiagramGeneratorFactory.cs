using Text2Diagram_Backend.Abstractions;
using Text2Diagram_Backend.Flowchart;

namespace Text2Diagram_Backend.Implementations;

public class DiagramGeneratorFactory : IDiagramGeneratorFactory
{
    private readonly Dictionary<DiagramType, IDiagramGenerator> generators;

    public DiagramGeneratorFactory(
        FlowchartDiagramGenerator flowchartDiagramGenerator)
    {
        generators = new Dictionary<DiagramType, IDiagramGenerator>
        {
            { DiagramType.Flowchart, flowchartDiagramGenerator }
        };
    }

    public IDiagramGenerator GetGenerator(DiagramType diagramType)
    {
        if (generators.TryGetValue(diagramType, out var generator))
        {
            return generator;
        }

        throw new ArgumentException("Invalid diagram type", nameof(diagramType));
    }
}