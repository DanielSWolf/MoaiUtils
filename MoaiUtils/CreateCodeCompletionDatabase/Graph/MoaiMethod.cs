using System.Collections.Generic;

namespace CreateCodeCompletionDatabase.Graph {
    public class MoaiMethod : MoaiTypeMember {
        public MoaiMethod() {
            Overrides = new List<MethodOverride>();
        }

        public bool IsStatic { get; set; }
        public List<MethodOverride> Overrides { get; private set; }

        public override string ToString() {
            return base.ToString() + "()";
        }
    }
}