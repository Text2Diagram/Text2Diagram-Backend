namespace Text2Diagram_Backend.Common.Abstractions;

public interface ISyntaxValidator
{
    Task<DiagramValidationResult> ValidateAsync(string code);
}
