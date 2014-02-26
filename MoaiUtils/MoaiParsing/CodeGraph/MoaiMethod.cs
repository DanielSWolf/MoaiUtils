using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing.CodeGraph {
    public class MoaiMethod : MoaiTypeMember {
        public MoaiMethod() {
            Overloads = new List<MoaiMethodOverload>();
        }

        public MethodPosition MethodPosition { get; set; }
        public List<MoaiMethodOverload> Overloads { get; private set; }
        public ISignature InParameterSignature { get; set; }
        public ISignature OutParameterSignature { get; set; }

        public override string ToString() {
            return base.ToString() + "()";
        }
    }
}