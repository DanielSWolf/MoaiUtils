using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class ClassParser {

        private static readonly Regex classDefinitionRegex = new Regex(@"
            # Documentation (optional)
            (?>
                /\*\*\s*
                    (?<annotation>@(\S@|\*(?!/)|[^@*])*)+
                \*/\s*
            )?

            # Class definition
            (class|struct)\s+
            (?<className>[A-Za-z0-9_]+)\s*
            (
                :\s*
                (
                    ((public|protected|private|virtual)\s*)+
                    (?<baseClassName>[A-Za-z0-9_:<,\s>]+?)\s*
                    ,?\s*
                )+
            )?
            \{
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public static void ParseClassDefinitions(string code, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            // Find all class definitions
            var matches = classDefinitionRegex.Matches(code);

            // Parse class definitions
            foreach (Match match in matches) {
                string className = match.Groups["className"].Value;
                var classPosition = new ClassPosition(filePosition, className);

                // Parse annotations, filtering out unknown ones
                Annotation[] annotations = match.Groups["annotation"].Captures
                    .Cast<Capture>()
                    .Select(capture => Annotation.Create(capture.Value, classPosition, warnings))
                    .ToArray();
                foreach (var unknownAnnotation in annotations.OfType<UnknownAnnotation>()) {
                    warnings.Add(classPosition, WarningType.UnexpectedAnnotation,
                        "Unknown annotation command '{0}'.", unknownAnnotation.Command);
                }
                annotations = annotations
                    .Where(annotation => !(annotation is UnknownAnnotation))
                    .ToArray();

                // Get base class names, ignoring all template classes and primitive types
                MoaiClass[] baseClasses = match.Groups["baseClassName"].Captures
                    .Cast<Capture>()
                    .Where(capture => !capture.Value.Contains("<"))
                    .Select(capture => types.GetOrCreate(capture.Value, null))
                    .OfType<MoaiClass>()
                    .ToArray();

                // Parse annotation block
                MoaiClass moaiClass = types.GetOrCreate(className, classPosition) as MoaiClass;
                if (moaiClass != null) {
                    moaiClass.ClassPosition = classPosition;
                    if (annotations.Any()) {
                        ParseClassDocumentation(moaiClass, annotations, baseClasses, classPosition, warnings);
                    }
                }
            }
        }

        private static void ParseClassDocumentation(MoaiClass moaiClass, Annotation[] annotations, MoaiClass[] baseClasses, ClassPosition classPosition, WarningList warnings) {
            // Check that there is a single @lua annotation
            int nameAnnotationCount = annotations.OfType<LuaNameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                warnings.Add(classPosition, WarningType.MissingAnnotation, "Missing @lua annotation.");
            } else if (nameAnnotationCount > 1) {
                warnings.Add(classPosition, WarningType.UnexpectedAnnotation, "Multiple @lua annotations.");
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
                if (annotation is LuaNameAnnotation) {
                    // Nothing to do. Name is already set. Just make sure the annotation is correct.
                    var nameAnnotation = (LuaNameAnnotation) annotation;
                    if (nameAnnotation.Value != moaiClass.Name) {
                        warnings.Add(classPosition, WarningType.UnexpectedValue,
                            "@lua annotation has inconsistent value '{0}'. Expected '{1}'.", nameAnnotation.Value, moaiClass.Name);
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