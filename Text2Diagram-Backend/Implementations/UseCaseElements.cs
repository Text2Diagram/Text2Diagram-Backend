namespace Text2Diagram_Backend.Common;

public record UseCaseElements(
    List<string> Actors,
    List<string> Triggers,
    List<string> Decisions,
    List<string> MainFlow,
    Dictionary<string, List<string>> AlternativeFlows,
    Dictionary<string, List<string>> ExceptionFlows);