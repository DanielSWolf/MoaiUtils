namespace CreateApiDescription.Graph {
    public class MoaiTypeMember : INamedEntity, IDocumentedEntity {
        public MoaiType OwningType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString() {
            return string.Format("{0}.{1}", OwningType.Name, Name);
        }
    }
}