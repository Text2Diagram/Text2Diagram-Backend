using Text2Diagram_Backend.Abstractions;
using Text2Diagram_Backend.Flowchart;
using Text2Diagram_Backend.UsecaseDiagram;

namespace Text2Diagram_Backend.Implementations;

public class DiagramGeneratorFactory : IDiagramGeneratorFactory
{
    private readonly Dictionary<DiagramType, IDiagramGenerator> generators;

    public DiagramGeneratorFactory(
        FlowchartDiagramGenerator flowchartDiagramGenerator, UsecaseDiagramGenerator usecaseDiagramGenerator)
    {
        generators = new Dictionary<DiagramType, IDiagramGenerator>
        {
            { DiagramType.Flowchart, flowchartDiagramGenerator },
            { DiagramType.UseCase, usecaseDiagramGenerator }
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