using Text2Diagram_Backend.Abstractions;

namespace Text2Diagram_Backend.Common;

public class MermaidSyntaxValidator : ISyntaxValidator
{
    public async Task<bool> ValidateAsync(string code)
    {
        return true;
    }
}
