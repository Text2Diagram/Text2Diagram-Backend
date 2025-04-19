using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Controllers;

public class TestController : Controller
{
    private readonly UseCaseSpecGenerator useCaseSpecGenerator;

    public TestController(UseCaseSpecGenerator useCaseSpecGenerator)
    {
        this.useCaseSpecGenerator = useCaseSpecGenerator;
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(string input)
    {
        var output = await useCaseSpecGenerator.GenerateUseCaseSpecAsync(input);
        return Ok(output);
    }
}
