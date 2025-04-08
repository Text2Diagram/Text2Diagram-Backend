using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Common.Abstractions;

public interface IDiagramGeneratorFactory
{
    IDiagramGenerator GetGenerator(DiagramType diagramType);
}
