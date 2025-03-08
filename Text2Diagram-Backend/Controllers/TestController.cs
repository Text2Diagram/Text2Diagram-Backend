using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.State;

namespace Text2Diagram_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IAnalyzer<StateElements> analyzer;

    public TestController(IAnalyzer<StateElements> analyzer)
    {
        this.analyzer = analyzer;
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromBody] UseCaseRequest request)
    {
        await analyzer.AnalyzeAsync(request.Input);
        return Ok();
    }
}

public record UseCaseRequest(string Input);
