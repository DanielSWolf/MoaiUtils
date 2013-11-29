namespace CreateApiDescription.Graph {
    public class MoaiReturnValue : IDocumentedEntity {
        public string Description { get; set; }
        public MoaiType Type { get; set; }
    }
}