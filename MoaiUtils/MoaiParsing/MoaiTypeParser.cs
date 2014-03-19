using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.MoaiParsing {
    public static class MoaiTypeParser {
        
        public static void ParseTypeDocumentation(MoaiType type, Annotation[] annotations, MoaiType[] baseTypes, TypePosition typePosition, WarningList warnings) {
            // Check that there is a single @name annotation
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                warnings.Add(typePosition, WarningType.MissingAnnotation, "Missing @name annotation.");
            } else if (nameAnnotationCount > 1) {
                warnings.Add(typePosition, WarningType.UnexpectedAnnotation, "Multiple @name annotations.");
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                warnings.Add(typePosition, WarningType.MissingAnnotation, "Missing @text annotation.");
            } else if (textAnnotationCount > 1) {
                warnings.Add(typePosition, WarningType.UnexpectedAnnotation, "Multiple @text annotations.");
            }

            // Store base types
            type.BaseTypes.AddRange(baseTypes);

            // Parse annotations
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Nothing to do. Name is already set. Just make sure the annotation is correct.
                    var nameAnnotation = (NameAnnotation) annotation;
                    if (nameAnnotation.Value != type.Name) {
                        warnings.Add(typePosition, WarningType.UnexpectedValue,
                            "@name annotation has inconsistent value '{0}'. Expected '{1}'.", nameAnnotation.Value, type.Name);
                    }
                } else if (annotation is TextAnnotation) {
                    // Set type description
                    type.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is FieldAnnotation) {
                    // Add field (constant, flag, or attribute)
                    var fieldAnnotation = (FieldAnnotation) annotation;
                    MoaiField field = (annotation is ConstantAnnotation) ? new MoaiConstant()
                        : (annotation is FlagAnnotation) ? (MoaiField) new MoaiFlag()
                        : new MoaiAttribute();
                    field.OwningType = type;
                    field.Name = fieldAnnotation.Name;
                    field.Description = fieldAnnotation.Description;
                    type.Members.Add(field);
                } else {
                    warnings.Add(typePosition, WarningType.UnexpectedAnnotation,
                        "Unexpected {0} annotation.", annotation.Command);
                }
            }
        }

    }
}