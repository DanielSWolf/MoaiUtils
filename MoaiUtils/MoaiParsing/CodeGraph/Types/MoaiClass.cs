using System.Collections.Generic;
using System.Linq;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {
    public class MoaiClass : IType {
        public MoaiClass() {
            Members = new List<ClassMember>();
            BaseClasses = new List<MoaiClass>();
            DocumentationReferences = new SortedSet<FilePosition>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public ClassPosition ClassPosition { get; set; }
        public bool IsScriptable { get; set; }

        public List<ClassMember> Members { get; private set; }
        public List<MoaiClass> BaseClasses { get; private set; }
        public SortedSet<FilePosition> DocumentationReferences { get; private set; }

        public string Signature {
            get {
                return string.Format(
                    BaseClasses.Any() ? "class {0} : {1}" : "class {0}",
                    Name, BaseClasses.Select(c => c.Name).Join(", "));
            }
        }

        public bool IsConfirmed {
            get { return IsDocumented; }
        }

        public IEnumerable<ClassMember> InheritedMembers {
            get {
                return BaseClasses
                    .SelectMany(baseType => baseType.AllMembers)
                    .Distinct(MemberNameEqualityComparer.Instance)
                    .Except(Members, MemberNameEqualityComparer.Instance);
            }
        }

        public IEnumerable<ClassMember> AllMembers {
            get { return Members.Concat(InheritedMembers); }
        }

        public IEnumerable<MoaiClass> AncestorClasses {
            get {
                return BaseClasses
                    .Concat(BaseClasses.SelectMany(c => c.AncestorClasses))
                    .Distinct();
            }
        }

        public override string ToString() {
            return Signature;
        }

        public bool IsDocumented {
            get { return Description != null || Members.Any(); }
        }

        private class MemberNameEqualityComparer : IEqualityComparer<ClassMember> {
            public static readonly MemberNameEqualityComparer Instance = new MemberNameEqualityComparer();

            private MemberNameEqualityComparer() { }

            public bool Equals(ClassMember x, ClassMember y) {
                return x.Name == y.Name;
            }

            public int GetHashCode(ClassMember obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}