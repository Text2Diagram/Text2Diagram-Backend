namespace Text2Diagram_Backend.State;

public record StateElements(
    List<string> States,
    List<string> Events,
    List<StateTransition> Transitions);

public record StateTransition(
    string Source,
    string Target,
    string Event,
    string Guard);
