using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace Text2Diagram_Backend.Data.Models;

public class Project
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    [Column(TypeName = "jsonb")]
    public Dictionary<string, JToken> Data { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    private Project() { }
}
