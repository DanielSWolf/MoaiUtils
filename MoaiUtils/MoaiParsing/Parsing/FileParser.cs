using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Parsing {
    public class FileParser {

        public static void ParseMoaiCodeFile(FileInfo codeFile, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            string code = codeFile.ReadAllText();
            ParseMoaiCodeFile(code, filePosition, types, warnings);
        }

        public static void ParseMoaiCodeFile(string code, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            ParseDocumentationBlocks(code, filePosition, types, warnings);
        }

        private static readonly Regex documentationRegex = new Regex(@"
            /\*\*\s*
                # Documentation
                (?<annotation>(?<!\S)@[\s\S]*?)+
            \*/\s*
            (
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
                    {
                )?
                |
                # Method definition
                int\s+(?<className>[A-Za-z0-9_]+)\s*::\s*(?<methodName>[A-Za-z0-9_]+)\s*\([^)]*\)\s*
                (
                    \{(?<methodBody>[\s\S]*?)(^\}|\}\s*//-----------------------------)
                )?
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private static void ParseDocumentationBlocks(string code, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            // Find all documentation blocks
            var matches = documentationRegex.Matches(code);

            // Parse documentation blocks
            foreach (Match match in matches) {
                // A documentation block may be attached to a class or to a method.
                string className = match.Groups["className"].Value;
                FilePosition documentationPosition = match.Groups["methodName"].Success
                    ? new MethodPosition(filePosition, className, match.Groups["methodName"].Value)
                    : new ClassPosition(filePosition, className);

                // Parse annotations, filtering out unknown ones
                Annotation[] annotations = match.Groups["annotation"].Captures
                    .Cast<Capture>()
                    .Select(capture => Annotation.Create(capture.Value, documentationPosition, warnings))
                    .ToArray();
                foreach (var unknownAnnotation in annotations.OfType<UnknownAnnotation>()) {
                    warnings.Add(documentationPosition, WarningType.UnexpectedAnnotation,
                        "Unknown annotation command '{0}'.", unknownAnnotation.Command);
                }
                annotations = annotations
                    .Where(annotation => !(annotation is UnknownAnnotation))
                    .ToArray();

                // Parse annotation block
                MoaiClass moaiClass = types.GetOrCreateClass(className, documentationPosition);
                if (documentationPosition is MethodPosition) {
                    // The documentation was attached to a method definition

                    // Get method body
                    string methodBody = match.Groups["methodBody"].Value;

                    MethodParser.ParseMethodDocumentation(moaiClass, annotations, methodBody, (MethodPosition) documentationPosition, types, warnings);
                } else {
                    // The documentation was attached to a class definition

                    // Get base class names, ignoring all template classes
                    MoaiClass[] baseClasses = match.Groups["baseClassName"].Captures
                        .Cast<Capture>()
                        .Where(capture => !capture.Value.Contains("<"))
                        .Select(capture => types.GetOrCreateClass(capture.Value, null))
                        .ToArray();

                    var classPosition = (ClassPosition) documentationPosition;
                    moaiClass.ClassPosition = classPosition;
                    ClassParser.ParseClassDocumentation(moaiClass, annotations, baseClasses, classPosition, warnings);
                }
            }
        }

    }
}