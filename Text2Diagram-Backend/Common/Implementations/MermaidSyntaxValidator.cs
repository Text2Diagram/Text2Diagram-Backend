using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Common.Implementations;

public class MermaidSyntaxValidator : ISyntaxValidator
{
    public async Task<bool> ValidateAsync(string code)
    {
        return true;
    }
}
