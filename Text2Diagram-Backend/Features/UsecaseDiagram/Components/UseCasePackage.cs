namespace Text2Diagram_Backend.Features.UsecaseDiagram.Components
{
    public class UseCasePackage
    {
        public string Name { get; set; } = string.Empty;
        public List<Actor> Actors { get; set; } = new();
        public List<UseCase> UseCases { get; set; } = new();
        public List<Association> Associations { get; set; } = new();
        public List<Extend> Extends { get; set; } = new();
        public List<Include> Includes { get; set; } = new();
    }
}
