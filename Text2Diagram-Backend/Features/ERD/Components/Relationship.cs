namespace Text2Diagram_Backend.Features.ERD.Components
{
	public class Relationship
	{
		public string source_entity_name { get; set; } = string.Empty;
		public string destination_entity_name { get;set; } = string.Empty;
		public RelationshipType source_relationship_name { get; set; }
		public RelationshipType destination_relationship_name { get; set; }
		public string decription {  get; set; } = string.Empty;
	}

	public enum RelationshipType
	{
		ZeroOrOne, //Zero or one
		ExactlyOne, // 	Exactly one
		ZeroOrMore,  // Zero or more (no upper limit)
		OneOrMore, //One or more (no upper limit)
	}
}
