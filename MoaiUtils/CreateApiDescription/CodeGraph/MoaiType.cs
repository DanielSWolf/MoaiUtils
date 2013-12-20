using System.Collections.Generic;
using System.Linq;
using Tools;

namespace CreateApiDescription.CodeGraph {
    public class MoaiType : INamedEntity, IDocumentedEntity {
        public MoaiType() {
            Members = new List<MoaiTypeMember>();
            BaseTypes = new List<MoaiType>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<MoaiTypeMember> Members { get; private set; }
        public List<MoaiType> BaseTypes { get; private set; }

        public string Signature {
            get {
                return string.Format(
                    BaseTypes.Any() ? "class {0} : {1}" : "class {0}",
                    Name, BaseTypes.Select(type => type.Name).Join(", "));
            }
        }

        public IEnumerable<MoaiTypeMember> InheritedMembers {
            get {
                return BaseTypes
                    .SelectMany(baseType => baseType.AllMembers)
                    .Distinct(MemberNameEqualityComparer.Instance)
                    .Except(Members, MemberNameEqualityComparer.Instance);
            }
        }

        public IEnumerable<MoaiTypeMember> AllMembers {
            get { return Members.Concat(InheritedMembers); }
        }

        public IEnumerable<MoaiType> AncestorTypes {
            get {
                return BaseTypes
                    .Concat(BaseTypes.SelectMany(baseType => baseType.AncestorTypes))
                    .Distinct();
            }
        }

        public override string ToString() {
            return Signature;
        }

        private class MemberNameEqualityComparer : IEqualityComparer<MoaiTypeMember> {
            public static MemberNameEqualityComparer Instance = new MemberNameEqualityComparer();

            private MemberNameEqualityComparer() {}

            public bool Equals(MoaiTypeMember x, MoaiTypeMember y) {
                return x.Name == y.Name;
            }

            public int GetHashCode(MoaiTypeMember obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}