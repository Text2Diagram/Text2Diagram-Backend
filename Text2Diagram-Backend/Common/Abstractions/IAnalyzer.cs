namespace Text2Diagram_Backend.Common.Abstractions;

public interface IAnalyzer<T>
{
    Task<T> AnalyzeAsync(string spec);
}
