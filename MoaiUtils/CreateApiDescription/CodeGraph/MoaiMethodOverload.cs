using System.Collections.Generic;

namespace CreateApiDescription.CodeGraph {
    public class MoaiMethodOverload {
        public MoaiMethodOverload() {
            InParameters = new List<MoaiInParameter>();
            OutParameters = new List<MoaiOutParameter>();
        }

        public MoaiMethod OwningMethod { get; set; }
        public List<MoaiInParameter> InParameters { get; private set; }
        public List<MoaiOutParameter> OutParameters { get; private set; }
    }
}