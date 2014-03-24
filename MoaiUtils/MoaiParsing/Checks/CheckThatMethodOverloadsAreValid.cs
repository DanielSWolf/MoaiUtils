using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatMethodOverloadsAreValid : CheckBase {

        public override void Run() {
            foreach (Method method in Methods) {
                // Check that there is at least one @out annotation per overload
                foreach (MethodOverload overload in method.Overloads) {
                    if (!overload.OutParameters.Any()) {
                        Warnings.Add(method.MethodPosition, WarningType.MissingAnnotation,
                            "Missing @out annotation. Even for void methods, an @out annotation with type nil is expected.");
                    }
                }

                // Make sure that no required parameter follows an optional one
                foreach (MethodOverload overload in method.Overloads) {
                    if (overload.InParameters.Pairwise().Any(pair => pair.Item1.IsOptional && !pair.Item2.IsOptional)) {
                        Warnings.Add(method.MethodPosition, WarningType.UnexpectedAnnotation,
                            "Required params (@in) are not allowed after optional params (@opt).");
                    }
                }

                // Check 'self' params
                foreach (var overload in method.Overloads) {
                    if (overload.InParameters.Skip(1).Any(param => param.Name == "self")) {
                        Warnings.Add(method.MethodPosition, WarningType.UnexpectedValue,
                            "'self' param must be at index 1.");
                    }
                    var firstInParam = overload.InParameters.FirstOrDefault();
                    if (firstInParam != null && firstInParam.Name == "self") {
                        if (firstInParam.Type != method.OwningClass) {
                            Warnings.Add(method.MethodPosition, WarningType.UnexpectedValue,
                                "'self' param is of type {0}. Expected {1}.",
                                firstInParam.Type != null ? firstInParam.Type.Name : "unknown", method.OwningClass.Name);
                        }
                    }
                }

            }
        }

    }
}