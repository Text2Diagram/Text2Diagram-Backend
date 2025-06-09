using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Controllers;

public class TestController : Controller
{
    private readonly TerminalNodesExtractor _terminalNodesExtractor;

    public TestController(TerminalNodesExtractor terminalNodesExtractor)
    {
        _terminalNodesExtractor = terminalNodesExtractor;
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(string input)
    {
        var output = await _terminalNodesExtractor.ExtractTerminalNodesAsync(input);
        return Ok(output);
    }
}
