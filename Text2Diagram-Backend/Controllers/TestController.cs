using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Common;

namespace Text2Diagram_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly UseCaseSpecAnalyzer useCaseSpecAnalyzer;

    public TestController(UseCaseSpecAnalyzer useCaseSpecAnalyzer)
    {
        this.useCaseSpecAnalyzer = useCaseSpecAnalyzer;
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromBody] UseCaseRequest request)
    {
        return Ok();
    }
}

public record UseCaseRequest(string UseCase);
