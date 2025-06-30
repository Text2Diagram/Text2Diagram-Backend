using System.Text.Json.Serialization;

namespace Text2Diagram_Backend.Features.ERD.Components;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RelationshipType
{
    ZeroOrOne,
    ExactlyOne,
    ZeroOrMore,
    OneOrMore,
}
