namespace MoaiUtils.CreateApiDescription.CodeGraph {
    public abstract class MoaiParameter : INamedEntity, IDocumentedEntity {
        public string Name { get; set; }
        public string Description { get; set; }
        public MoaiType Type { get; set; }

        public override string ToString() {
            return string.Format("{0} {1}", Type, Name);
        }
    }
}