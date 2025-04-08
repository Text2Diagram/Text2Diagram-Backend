namespace Text2Diagram_Backend.Abstractions;

public record DiagramValidationResult(bool IsValid, string ErrorMessage)
{
    public static DiagramValidationResult Valid() => new(true, string.Empty);
    public static DiagramValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
