using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatReferencedClassesExist : CheckBase {
        public override void Run() {
            var missingClassesReferencedInDocumentation = Classes
                .Where(c => c.DocumentationReferences.Any())
                .Where(c => c.ClassPosition == null);
            foreach (MoaiClass moaiClass in missingClassesReferencedInDocumentation) {
                // There is no code file containing this class

                // Make an educated guess as to what type was meant.
                IType typeProposal = Types.Find(moaiClass.Name,
                    MatchMode.FindSynonyms | MatchMode.FindSimilar, t => t.Exists);

                foreach (FilePosition referencingFilePosition in moaiClass.DocumentationReferences) {
                    string message = string.Format(
                        "Documentation mentions missing type '{0}'.", moaiClass.Name);
                    if (typeProposal != null) {
                        message += string.Format(" Should this be '{0}'?", typeProposal.Name);
                    }
                    Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
                }
            }
        }
    }
}