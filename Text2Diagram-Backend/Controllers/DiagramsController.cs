using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.Controllers;

public record GenerateDiagramRequest(string DiagramType, string Input);

[ApiController]
[Route("[controller]")]
public class DiagramsController : ControllerBase
{
    private readonly IDiagramGeneratorFactory generatorFactory;

    public DiagramsController(IDiagramGeneratorFactory generatorFactory)
    {
        this.generatorFactory = generatorFactory;
    }

    [HttpPost]

    public async Task<IActionResult> GenerateDiagram([FromBody] GenerateDiagramRequest request)
    {
        DiagramType diagramType;
        var parseSuccess = Enum.TryParse(request.DiagramType, true, out diagramType);

        if (!parseSuccess)
        {
            return BadRequest("Invalid diagram type.");
        }

        var diagramGenerator = generatorFactory.GetGenerator(diagramType);
        var result = await diagramGenerator.GenerateAsync(request.Input);
        return Ok(result);
    }

}
