using System.Text.Json;

namespace Text2Diagram_Backend.Features.Flowchart;

public class FlowJsonSerializer
{
    public static string SerializeFlowsToJson(List<Flow> flows, ILogger logger)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(flows, options);
            logger.LogInformation("{JsonString}", jsonString);
            return jsonString;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serialize flows to JSON");
            return string.Empty;
        }
    }
}
