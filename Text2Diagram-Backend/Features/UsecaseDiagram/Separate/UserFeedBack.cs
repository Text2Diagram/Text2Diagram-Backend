namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public enum InstructionAction
    {
        Add,
        Remove,
        Update
    }

    public enum InstructionTarget
    {
        Actor,
        UseCase,
        Association,
        Include,
        Extend,
        Package
    }
    public class UserFeedBack
    {
        public InstructionAction Action { get; init; }
        public InstructionTarget Target { get; init; }
        // Common properties
        public string? Name { get; set; }
        public string? NewName { get; set; }

        // For Association
        public string? Actor { get; set; }
        public string? UseCase { get; set; }

        // For Include
        public string? BaseUseCase { get; set; }
        public string? IncludedUseCase { get; set; }

        // For Extend
        public string? ExtendedUseCase { get; set; }

        // For Package
        public List<string>? UseCases { get; set; }
        public List<string>? Actors { get; set; }
    }
}
