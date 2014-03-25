using System;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {
    public class PrimitiveLuaType : IType {
        public PrimitiveLuaType(string name) {
            Name = name;
        }

        public string Name { get; private set; }

        public string Description {
            get { return String.Format("A primitive Lua {0}", Name); }
        }

        public string Signature {
            get { return Name; }
        }

        public bool Exists {
            get { return true; }
        }

        public override string ToString() {
            return Signature;
        }

    }
}