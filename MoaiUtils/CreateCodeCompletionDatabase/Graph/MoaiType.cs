using System.Collections.Generic;

namespace CreateCodeCompletionDatabase.Graph {
    public class MoaiType : INamedEntity, IDocumentedEntity {
        public MoaiType() {
            Members = new List<MoaiTypeMember>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<MoaiTypeMember> Members { get; private set; }

        public override string ToString() {
            return Name;
        }
    }
}