namespace CreateApiDescription.Graph {
    public class MoaiParameter : INamedEntity, IDocumentedEntity {
        public string Name { get; set; }
        public string Description { get; set; }
        public MoaiType Type { get; set; }
        public bool IsOptional { get; set; }
    }
}