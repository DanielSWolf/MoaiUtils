using System.Collections.Generic;
using System.Linq;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {
    public class MoaiClass : IType, IDocumentationReferenceAware {
        private readonly SortedSet<FilePosition> documentationReferences = new SortedSet<FilePosition>();

        public MoaiClass() {
            Members = new List<ClassMember>();
            BaseClasses = new List<MoaiClass>();
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public ClassPosition ClassPosition { get; set; }
        public bool IsScriptable { get; set; }

        public List<ClassMember> Members { get; private set; }
        public List<MoaiClass> BaseClasses { get; private set; }

        public string Signature {
            get {
                return string.Format(
                    BaseClasses.Any() ? "class {0} : {1}" : "class {0}",
                    Name, BaseClasses.Select(c => c.Name).Join(", "));
            }
        }

        public bool Exists {
            get { return ClassPosition != null; }
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

        public IEnumerable<FilePosition> DocumentationReferences {
            get { return documentationReferences; }
        }

        public void AddDocumentationReference(FilePosition position) {
            documentationReferences.Add(position);
        }
    }
}