using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Controllers;

public record GenerateDiagramRequest(
    string DiagramType,
    string? TextInput,
    IFormFile? FileInput);

[ApiController]
[Route("[controller]")]
public class GeneratorsController : ControllerBase
{
    private readonly IDiagramGeneratorFactory generatorFactory;
    private readonly UseCaseSpecGenerator useCaseSpecGenerator;

    public GeneratorsController(
        IDiagramGeneratorFactory generatorFactory,
        UseCaseSpecGenerator useCaseSpecGenerator)
    {
        this.generatorFactory = generatorFactory;
        this.useCaseSpecGenerator = useCaseSpecGenerator;
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDiagram([FromForm] GenerateDiagramRequest request)
    {
        DiagramType diagramType;
        if (!Enum.TryParse(request.DiagramType, true, out diagramType))
        {
            return BadRequest("Invalid diagram type.");
        }

        if (string.IsNullOrEmpty(request.TextInput) && request.FileInput == null)
        {
            return BadRequest("TextInput or FileInput must be provided.");
        }

        if (!string.IsNullOrEmpty(request.TextInput) && request.FileInput != null)
        {
            return BadRequest("Only provide TextInput or FileInput.");
        }

        string input = string.Empty;

        if (!string.IsNullOrEmpty(request.TextInput))
        {
            input = request.TextInput.Trim();
        }
        else if (request.FileInput != null)
        {
            var extension = Path.GetExtension(request.FileInput.FileName);
            if (extension == ".txt")
            {
                using var stream = new MemoryStream();
                await request.FileInput.CopyToAsync(stream);
                input = Encoding.UTF8.GetString(stream.ToArray());
            }
            else if (extension == ".pdf")
            {
                using var stream = new MemoryStream();
                await request.FileInput.CopyToAsync(stream);
                stream.Position = 0;
                using var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream);
                var textBuilder = new StringBuilder();

                foreach (var page in pdfDocument.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }

                input = textBuilder.ToString();
            }
            else if (extension == ".docx")
            {
                using var stream = new MemoryStream();
                await request.FileInput.CopyToAsync(stream);
                stream.Position = 0;

                using var wordDoc = WordprocessingDocument.Open(stream, false);
                var body = wordDoc.MainDocumentPart?.Document.Body;
                input = body?.InnerText ?? string.Empty;
            }
            else
            {
                return BadRequest("Invalid FileInput format.");
            }
        }

        if (string.IsNullOrEmpty(input))
            return BadRequest("Empty file");

        var diagramGenerator = generatorFactory.GetGenerator(diagramType);
        var result = await diagramGenerator.GenerateAsync(input);
        return Ok(result);
    }

    [HttpPost("usecasespec")]
    public async Task<IActionResult> GenerateUseCaseSpec([FromBody] string input)
    {
        var result = await useCaseSpecGenerator.GenerateUseCaseSpecAsync(input);
        return Ok(result);
    }

}