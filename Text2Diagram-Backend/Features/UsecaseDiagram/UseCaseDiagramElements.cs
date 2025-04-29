namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public record UseCaseDiagramElements(
    List<string> Actors,
    List<string> UseCases,
    Dictionary<string, List<string>> Associations,
    Dictionary<string, List<string>> Includes,
    Dictionary<string, List<string>> Extends,
    Dictionary<string, List<string>> Groups);