using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.ERD.Components;
using Text2Diagram_Backend.Features.Sequence.Components;

namespace Text2Diagram_Backend.Features.Sequence;


public class SequenceDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<SequenceDiagramGenerator> logger;
    private readonly AnalyzerForSequence analyzer;

    public SequenceDiagramGenerator(
        ILogger<SequenceDiagramGenerator> logger,
        AnalyzerForSequence analyzer)
    {
        this.logger = logger;
        this.analyzer = analyzer;
    }

    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeAsync(input);

            // Generate Mermaid syntax
            string mermaidCode = GenerateMermaidCode(diagram);

            logger.LogInformation("Generated Mermaid code:\n{mermaidCode}", mermaidCode);

            return mermaidCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating flowchart diagram");
            throw;
        }
    }

	/// <summary>
	/// Generates Mermaid.js compatible syntax for the ER diagram.
	/// </summary>
	/// <param name="diagram">The ER diagram object containing entities and relationships</param>
	/// <returns>A string containing Mermaid.js ER diagram syntax</returns>
	private string GenerateMermaidCode(SequenceDiagram diagram)
	{
		var sb = new StringBuilder();
		sb.AppendLine("sequenceDiagram");

		foreach (var element in diagram.Elements)
		{
			try
			{
				switch (element)
				{
					case Statement statement:
						string from = statement.Participant1?.Trim();
						string to = statement.Participant2?.Trim();
						string message = statement.Message?.Trim();
						string arrow = string.IsNullOrEmpty(statement.ArrowType) ? "->>" : statement.ArrowType;

						if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(message))
						{
							sb.AppendLine($"    {from} {arrow} {to}: {message}");
						}
						break;

					case AltBlock altBlock:
						AppendAltBlock(sb, altBlock);
						break;

					case LoopBlock loopBlock:
						AppendLoopBlock(sb, loopBlock);
						break;

					case CriticalBlock criticalBlock:
						AppendCriticalBlock(sb, criticalBlock);
						break;

					case ParallelBlock parallelBlock:
						AppendParallelBlock(sb, parallelBlock);
						break;

					default:
						// Bỏ qua nếu không phải kiểu được hỗ trợ
						break;
				}
			}
			catch
			{
				// Bỏ qua phần tử lỗi, không làm gián đoạn việc sinh Mermaid code
				continue;
			}
		}

		return sb.ToString();
	}

	private void AppendAltBlock(StringBuilder sb, AltBlock altBlock)
	{
		sb.AppendLine("    alt " + altBlock.Branches.FirstOrDefault()?.Condition ?? "condition");
		foreach (var branch in altBlock.Branches)
		{
			if (branch != altBlock.Branches.First()) sb.AppendLine("    else " + branch.Condition);
			foreach (var inner in branch.Body)
			{
				var innerDiagram = new SequenceDiagram { Elements = new List<SequenceElement> { inner } };
				sb.Append(GenerateMermaidCode(innerDiagram));
			}
		}
		sb.AppendLine("    end");
	}

	private void AppendLoopBlock(StringBuilder sb, LoopBlock loopBlock)
	{
		sb.AppendLine("    loop " + loopBlock.Title);
		foreach (var inner in loopBlock.Body)
		{
			var innerDiagram = new SequenceDiagram { Elements = new List<SequenceElement> { inner } };
			sb.Append(GenerateMermaidCode(innerDiagram));
		}
		sb.AppendLine("    end");
	}

	private void AppendCriticalBlock(StringBuilder sb, CriticalBlock criticalBlock)
	{
		sb.AppendLine("    critical " + criticalBlock.Title ?? "Critical Section");
		foreach (var inner in criticalBlock.Body)
		{
			var innerDiagram = new SequenceDiagram { Elements = new List<SequenceElement> { inner } };
			sb.Append(GenerateMermaidCode(innerDiagram));
		}
		sb.AppendLine("    end");
	}

	private void AppendParallelBlock(StringBuilder sb, ParallelBlock parallelBlock)
	{
		sb.AppendLine("    par " + parallelBlock.Branches.FirstOrDefault()?.Title ?? "parallel");
		foreach (var branch in parallelBlock.Branches)
		{
			if (branch != parallelBlock.Branches.First()) sb.AppendLine("    and " + branch.Title);
			foreach (var inner in branch.Body)
			{
				var innerDiagram = new SequenceDiagram { Elements = new List<SequenceElement> { inner } };
				sb.Append(GenerateMermaidCode(innerDiagram));
			}
		}
		sb.AppendLine("    end");
	}
}