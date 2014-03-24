using System;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {
    public class Ellipsis : IType {
        public Ellipsis(IType type) {
            if (type == null) throw new ArgumentNullException("type");
            Type = type;
        }

        public IType Type { get; private set; }

        public string Name {
            get { return Type is Variant ? "..." : string.Format("{0}...", Type); }
        }

        public string Description {
            get { return string.Format("Any number of {0} elements", Type); }
        }

        public string Signature {
            get { return Name; }
        }

        public bool IsConfirmed {
            get { return Type.IsConfirmed; }
        }

        public override string ToString() {
            return Signature;
        }

        #region Equality members

        protected bool Equals(Ellipsis other) {
            return Type.Equals(other.Type);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Ellipsis) obj);
        }

        public override int GetHashCode() {
            return Type.GetHashCode();
        }

        #endregion
    }
}