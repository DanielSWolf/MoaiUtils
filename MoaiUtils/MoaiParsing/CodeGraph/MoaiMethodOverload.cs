using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing.CodeGraph {
    public class MoaiMethodOverload {
        public MoaiMethodOverload() {
            InParameters = new List<MoaiInParameter>();
            OutParameters = new List<MoaiOutParameter>();
        }

        public MoaiMethod OwningMethod { get; set; }
        public bool IsStatic { get; set; }
        public List<MoaiInParameter> InParameters { get; private set; }
        public List<MoaiOutParameter> OutParameters { get; private set; }
    }
}