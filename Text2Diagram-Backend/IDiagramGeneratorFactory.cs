namespace Text2Diagram_Backend;

public interface IDiagramGeneratorFactory
{
    IDiagramGenerator GetGenerator(DiagramType diagramType);
}
