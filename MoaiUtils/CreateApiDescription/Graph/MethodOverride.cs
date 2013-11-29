using System.Collections.Generic;

namespace CreateApiDescription.Graph {
    public class MethodOverride {
        public MethodOverride() {
            Parameters = new List<MoaiParameter>();
            ReturnValues = new List<MoaiReturnValue>();
        }

        public MoaiMethod OwningMethod { get; set; }
        public List<MoaiParameter> Parameters { get; private set; }
        public List<MoaiReturnValue> ReturnValues { get; private set; }
    }
}