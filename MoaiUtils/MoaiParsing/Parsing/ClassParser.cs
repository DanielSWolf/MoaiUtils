using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class ClassParser {
        
        public static void ParseClassDocumentation(MoaiClass moaiClass, Annotation[] annotations, MoaiClass[] baseClasses, ClassPosition classPosition, WarningList warnings) {
            // Check that there is a single @name annotation
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                warnings.Add(classPosition, WarningType.MissingAnnotation, "Missing @name annotation.");
            } else if (nameAnnotationCount > 1) {
                warnings.Add(classPosition, WarningType.UnexpectedAnnotation, "Multiple @name annotations.");
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                warnings.Add(classPosition, WarningType.MissingAnnotation, "Missing @text annotation.");
            } else if (textAnnotationCount > 1) {
                warnings.Add(classPosition, WarningType.UnexpectedAnnotation, "Multiple @text annotations.");
            }

            // Store base classes
            moaiClass.BaseClasses.AddRange(baseClasses);

            // Parse annotations
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Nothing to do. Name is already set. Just make sure the annotation is correct.
                    var nameAnnotation = (NameAnnotation) annotation;
                    if (nameAnnotation.Value != moaiClass.Name) {
                        warnings.Add(classPosition, WarningType.UnexpectedValue,
                            "@name annotation has inconsistent value '{0}'. Expected '{1}'.", nameAnnotation.Value, moaiClass.Name);
                    }
                } else if (annotation is TextAnnotation) {
                    // Set class description
                    moaiClass.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is FieldAnnotation) {
                    // Add field (constant, flag, or attribute)
                    var fieldAnnotation = (FieldAnnotation) annotation;
                    Field field = (annotation is ConstantAnnotation) ? new Constant()
                        : (annotation is FlagAnnotation) ? (Field) new Flag()
                        : new Attribute();
                    field.OwningClass = moaiClass;
                    field.Name = fieldAnnotation.Name;
                    field.Description = fieldAnnotation.Description;
                    moaiClass.Members.Add(field);
                } else {
                    warnings.Add(classPosition, WarningType.UnexpectedAnnotation,
                        "Unexpected {0} annotation.", annotation.Command);
                }
            }
        }

    }
}