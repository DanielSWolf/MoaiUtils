using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.CodeGraph {
    public abstract class ClassMember {
        public MoaiClass OwningClass { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString() {
            return string.Format("{0}.{1}", OwningClass.Name, Name);
        }
    }
}