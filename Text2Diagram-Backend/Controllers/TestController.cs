using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Features.Flowchart.Agents;

namespace Text2Diagram_Backend.Controllers;

public class TestController : Controller
{
    private readonly BasicFlowExtractor _basicFlowExtractor;

    public TestController(BasicFlowExtractor basicFlowExtractor)
    {
        _basicFlowExtractor = basicFlowExtractor;
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(string input)
    {
        var output = await _basicFlowExtractor.ExtractBasicFlowAsync(input);
        return Ok(output);
    }
}
