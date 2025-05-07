namespace Text2Diagram_Backend.Features.UsecaseDiagram.Components
{
    public record UseCaseDiagram
    (

        List<string> Actors,
        List<string> UseCases,
        List<Association> Associations,
        List<Extend> Extends,
        List<Include> Includes,
        //List<UseCaseGroup> Groups,
        List<UseCasePackage> Packages
        //List<Boundary> Boundaries
    );
}
