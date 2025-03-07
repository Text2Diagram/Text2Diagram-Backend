namespace Text2Diagram_Backend.Abstractions;

public interface ISyntaxValidator
{
    Task<bool> ValidateAsync(string code);
}
