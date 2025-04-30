using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Controllers;

public record GenerateDiagramRequest(string DiagramType, string Input, string? InputType);

[ApiController]
[Route("[controller]")]
public class GeneratorsController : ControllerBase
{
    private readonly IDiagramGeneratorFactory generatorFactory;

    public GeneratorsController(IDiagramGeneratorFactory generatorFactory)
    {
        this.generatorFactory = generatorFactory;
    }

    [HttpPost]

    public async Task<IActionResult> GenerateDiagram([FromBody] GenerateDiagramRequest request)
    {
        DiagramType diagramType;
        if (!Enum.TryParse(request.DiagramType, true, out diagramType))
        {
            return BadRequest("Invalid diagram type.");
        }

        var diagramGenerator = generatorFactory.GetGenerator(diagramType);
        var result = await diagramGenerator.GenerateAsync(request.Input);
        return Ok(FormatData.FormatDataFunc(0,0,0,result));
    }

}