using Text2Diagram_Backend.Data.Models;

namespace Text2Diagram_Backend.ViewModels
{
    public class DiagramVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiagramData { get; set; } = string.Empty;
        public DiagramType DiagramType { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; set; }
        public string UserId { get; init; } = string.Empty;
        public Guid ProjectId { get; init; }
        public string DiagramJson { get; set; } = string.Empty;
    }
}
