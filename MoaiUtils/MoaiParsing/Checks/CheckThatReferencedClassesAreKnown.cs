using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatReferencedClassesAreKnown : CheckBase {
        public override void Run() {
            IEnumerable<MoaiClass> classesReferencedInDocumentation = Classes
                .Where(type => type.DocumentationReferences.Any());
            foreach (MoaiClass moaiClass in classesReferencedInDocumentation.ToArray()) {
                WarnIfUnconfirmed(moaiClass);
            }
        }

        private void WarnIfUnconfirmed(MoaiClass moaiClass) {
            if (moaiClass.IsConfirmed) return;

            // Make an educated guess as to what type was meant.
            IType typeProposal = Types.Find(moaiClass.Name, MatchMode.FindSynonyms | MatchMode.FindSimilar, t => t.IsConfirmed);

            foreach (FilePosition referencingFilePosition in moaiClass.DocumentationReferences) {
                string message = string.Format(
                    "Documentation mentions missing or undocumented type '{0}'.", moaiClass.Name);
                if (typeProposal != null) {
                    message += string.Format(" Should this be '{0}'?", typeProposal.Name);
                }
                Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
            }
        }

    }
}