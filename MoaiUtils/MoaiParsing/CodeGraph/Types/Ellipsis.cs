using System;
using System.Collections.Generic;
using System.Linq;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {
    public class Ellipsis : IType, IDocumentationReferenceAware {
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

        public bool Exists {
            get { return Type.Exists; }
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

        public IEnumerable<FilePosition> DocumentationReferences {
            get {
                return (Type is IDocumentationReferenceAware)
                    ? ((IDocumentationReferenceAware) Type).DocumentationReferences
                    : Enumerable.Empty<FilePosition>();
            }
        }

        public void AddDocumentationReference(FilePosition position) {
            if (Type is IDocumentationReferenceAware) {
                ((IDocumentationReferenceAware) Type).AddDocumentationReference(position);
            }
        }
    }
}