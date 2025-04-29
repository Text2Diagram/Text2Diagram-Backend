using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Features.ERD;
using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Common.Implementations;

public class DiagramGeneratorFactory : IDiagramGeneratorFactory
{
    private readonly Dictionary<DiagramType, IDiagramGenerator> generators;

    public DiagramGeneratorFactory(
        FlowchartDiagramGenerator flowchartDiagramGenerator,
        ERDiagramGenerator eRDiagramGenerator)
    {
        generators = new Dictionary<DiagramType, IDiagramGenerator>
        {
            {
                DiagramType.Flowchart, flowchartDiagramGenerator
            },

			{
				DiagramType.ER, eRDiagramGenerator
			}
		};
    }

    public IDiagramGenerator GetGenerator(DiagramType diagramType)
    {
        if (generators.TryGetValue(diagramType, out var generator))
        {
            return generator;
        }

        throw new ArgumentException($"No generator found for diagram type {diagramType}.");
    }
}