namespace Text2Diagram_Backend.Common.Abstractions;

public interface IDiagramGeneratorFactory
{
    IDiagramGenerator GetGenerator(DiagramType diagramType);
}
