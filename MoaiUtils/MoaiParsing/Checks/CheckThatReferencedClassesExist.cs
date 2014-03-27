using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatReferencedClassesExist : CheckBase {
        public override void Run() {
            var referencedClasses = Classes.Where(c => c.DocumentationReferences.Any());
            foreach (MoaiClass moaiClass in referencedClasses) {
                if (!moaiClass.Exists) {
                    // Make an educated guess as to what type was meant.
                    IType typeProposal = Types.Find(moaiClass.Name,
                        MatchMode.FindSynonyms | MatchMode.FindSimilar, t => t.Exists);

                    foreach (FilePosition referencingFilePosition in moaiClass.DocumentationReferences) {
                        string message = string.Format(
                            "Documentation mentions unknown type '{0}'.", moaiClass.Name);
                        if (typeProposal != null) {
                            message += string.Format(" Should this be '{0}'?", typeProposal.Name);
                        }
                        Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
                    }
                }
            }
        }
    }
}