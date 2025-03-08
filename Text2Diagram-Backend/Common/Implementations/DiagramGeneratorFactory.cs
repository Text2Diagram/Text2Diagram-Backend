using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Flowchart;
using Text2Diagram_Backend.State;

namespace Text2Diagram_Backend.Common.Implementations;

public class DiagramGeneratorFactory : IDiagramGeneratorFactory
{
    private readonly Dictionary<DiagramType, IDiagramGenerator> generators;

    public DiagramGeneratorFactory(
        FlowchartDiagramGenerator flowchartDiagramGenerator,
        StateDiagramGenerator stateDiagramGenerator)
    {
        generators = new Dictionary<DiagramType, IDiagramGenerator>
        {
            { DiagramType.Flowchart, flowchartDiagramGenerator },
            //{ DiagramType.Sequence,  },
            //{ DiagramType.Class,  },
            //{ DiagramType.UseCase,  },
            { DiagramType.State, stateDiagramGenerator },
            //{ DiagramType.ER,  }
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