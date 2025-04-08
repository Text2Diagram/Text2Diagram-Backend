namespace Text2Diagram_Backend.Abstractions;

public interface ISyntaxValidator
{
    Task<DiagramValidationResult> ValidateAsync(string code);
}
