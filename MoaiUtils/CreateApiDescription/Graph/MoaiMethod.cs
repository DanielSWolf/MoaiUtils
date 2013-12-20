﻿using System.Collections.Generic;

namespace CreateApiDescription.Graph {
    public class MoaiMethod : MoaiTypeMember {
        public MoaiMethod() {
            Overloads = new List<MoaiMethodOverload>();
        }

        public bool IsStatic { get; set; }
        public List<MoaiMethodOverload> Overloads { get; private set; }
        public ISignature InParameterSignature { get; set; }
        public ISignature OutParameterSignature { get; set; }

        public override string ToString() {
            return base.ToString() + "()";
        }
    }
}