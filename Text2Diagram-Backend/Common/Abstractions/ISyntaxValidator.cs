namespace Text2Diagram_Backend.Common.Abstractions;

public interface ISyntaxValidator
{
    Task<bool> ValidateAsync(string code);
}
