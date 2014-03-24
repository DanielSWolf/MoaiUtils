using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatReferencedTypesAreKnown : CheckBase {
        public override void Run() {
            IEnumerable<Type> typesReferencedInDocumentation = Types
                .Where(type => type.DocumentationReferences.Any());
            foreach (Type type in typesReferencedInDocumentation.ToArray()) {
                WarnIfSpeculative(type);
            }
        }

        private void WarnIfSpeculative(Type type) {
            if (type.Name == "...") return;

            if (type.Name.EndsWith("...")) {
                type = Types.GetOrCreate(type.Name.Substring(0, type.Name.Length - 3), null);
            }

            if (!type.IsDocumented && !type.IsPrimitive) {
                // Make an educated guess as to what type was meant.
                Type typeProposal = Types.Find(type.Name,
                    MatchMode.FindSynonyms, t => t.IsDocumented || t.IsPrimitive);

                foreach (FilePosition referencingFilePosition in type.DocumentationReferences) {
                    string message = string.Format(
                        "Documentation mentions missing or undocumented type '{0}'.", type.Name);
                    if (typeProposal != null) {
                        message += string.Format(" Should this be '{0}'?", typeProposal.Name);
                    }
                    Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
                }
            }
        }

    }
}