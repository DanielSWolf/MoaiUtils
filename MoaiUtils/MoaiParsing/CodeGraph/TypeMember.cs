namespace MoaiUtils.MoaiParsing.CodeGraph {
    public abstract class TypeMember {
        public Type OwningType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString() {
            return string.Format("{0}.{1}", OwningType.Name, Name);
        }
    }
}