using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing.CodeGraph {
    public class Method : TypeMember {
        public Method() {
            Overloads = new List<MethodOverload>();
        }

        public MethodPosition MethodPosition { get; set; }
        public List<MethodOverload> Overloads { get; private set; }
        public ISignature InParameterSignature { get; set; }
        public ISignature OutParameterSignature { get; set; }
        public string Body { get; set; }

        public override string ToString() {
            return base.ToString() + "()";
        }
    }
}