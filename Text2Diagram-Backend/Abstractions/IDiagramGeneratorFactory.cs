namespace Text2Diagram_Backend.Abstractions;

public interface IDiagramGeneratorFactory
{
    IDiagramGenerator GetGenerator(DiagramType diagramType);
}
