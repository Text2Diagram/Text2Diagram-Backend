namespace Text2Diagram_Backend.Features.ERD.Components;

public class Entity
{
    public string Name { get; set; } = string.Empty;
    public List<Property> Properties { get; set; } = [];
}
