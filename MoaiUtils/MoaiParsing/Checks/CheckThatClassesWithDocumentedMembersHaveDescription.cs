using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatClassesWithDocumentedMembersHaveDescription : CheckBase {
        public override void Run() {
            var classesWithMembers = Classes.Where(c => c.Members.Any());
            foreach (MoaiClass moaiClass in classesWithMembers) {
                if (moaiClass.Description == null) {
                    Warnings.Add(moaiClass.ClassPosition, WarningType.MissingAnnotation,
                        string.Format("Class '{0}' has documented members but is missing a @text description.", moaiClass.Name));
                }
            }
        }
    }
}