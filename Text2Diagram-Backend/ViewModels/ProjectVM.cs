using Newtonsoft.Json.Linq;

namespace Text2Diagram_Backend.Data.Models;

public class ProjectVM
{
    public string UserId { set; get; } = string.Empty;
    public Dictionary<string, JToken> Data { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Thumbnail { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
