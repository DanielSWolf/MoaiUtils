using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;

namespace MoaiUtils.DocExport.Exporters {

    public class MarkdownExporter : IApiExporter {

        public void Export(MoaiClass[] classes, string header, DirectoryInfo outputDirectory) {
            // Create info records for all classes
            var classesByName = classes.ToDictionary(moaiClass => moaiClass.Name);

            foreach (MoaiClass moaiClass in classes) {
                ExportClass(moaiClass, outputDirectory, classesByName);
            }
        }

        private void ExportClass(MoaiClass moaiClass, DirectoryInfo outputRootDirectory, Dictionary<string, MoaiClass> classesByName) {
            FileInfo fileInfo = moaiClass.GetFileInfo(outputRootDirectory);
            fileInfo.Directory.Create();

            using (var file = File.CreateText(fileInfo.FullName)) {
                // Header
                file.WriteLine("# Class `{0}`\n", moaiClass.Name);
                file.WriteLine("## Overview\n");
                file.WriteLine("Base classes: {0}\n", moaiClass.BaseClasses.Any()
                    ? moaiClass.BaseClasses.OrderBy(c => c.Name).Select(moaiClass.GetLinkTo).Join(", ")
                    : "*None*");
                var derivedClasses = classesByName.Values
                    .Where(c => c.BaseClasses.Contains(moaiClass))
                    .OrderBy(c => c.Name)
                    .ToList();
                file.WriteLine("Derived classes: {0}\n", derivedClasses.Any() ? derivedClasses.Select(moaiClass.GetLinkTo).Join(", ") : "*None*");
                
                // Description
                QuoteDescription(moaiClass.Description, file, moaiClass, classesByName);

                // Fields
                var fieldTypes = new[] {
                    new { Description = "Constants", Type = typeof(Constant) },
                    new { Description = "Flags", Type = typeof(Flag) },
                    new { Description = "Attributes", Type = typeof(Attribute) }
                };
                foreach (var fieldType in fieldTypes) {
                    var fields = moaiClass.Members
                        .Where(member => fieldType.Type.IsInstanceOfType(member))
                        .Cast<Field>()
                        .OrderBy(f => f.Name)
                        .ToList();
                    if (fields.Any()) {
                        file.WriteLine("## {0}\n", fieldType.Description);
                        foreach (Field field in fields) {
                            file.WriteLine("#### `{0}`\n", field.Name);
                            QuoteDescription(field.Description, file, moaiClass, classesByName);
                        }
                    }
                }

                // Methods
                var methods = moaiClass.Members.OfType<Method>().ToList();
                if (methods.Any()) {
                    file.WriteLine("## Methods\n");
                    foreach (Method method in methods.OrderBy(m => m.Name)) {
                        file.WriteLine("#### `{0}`\n", method.Name);
                        string signature = string.Format(
                            "{0} -> {1}",
                            method.InParameterSignature != null ? method.InParameterSignature.ToString(SignatureGrouping.Any) : "?",
                            method.OutParameterSignature != null ? method.OutParameterSignature.ToString(SignatureGrouping.Any) : "?");
                        QuoteDescription(signature, file, moaiClass, classesByName);
                        QuoteDescription(method.Description, file, moaiClass, classesByName);
                    }
                }
            }
        }

        private static void QuoteDescription(string description, StreamWriter file, MoaiClass currentClass, Dictionary<string, MoaiClass> classesByName) {
            if (description != null) {
                foreach (string line in description.SplitIntoLines()) {
                    string outLine = Regex.Replace(line, @"\bMOAI[a-zA-Z0-9_]+\b", match => {
                        MoaiClass moaiClass;
                        return (classesByName.TryGetValue(match.Value, out moaiClass))
                            ? currentClass.GetLinkTo(moaiClass)
                            : match.Value;
                    });
                    file.WriteLine("> {0}\n", outLine);
                }
            }
        }

    }

    public static class MoaiClassExtensions {
        public static string GetFileName(this MoaiClass moaiClass) {
            return moaiClass.Name + ".md";
        }

        public static string GetDirectoryString(this MoaiClass moaiClass) {
            string sourceFilePath = moaiClass.ClassPosition != null ? moaiClass.ClassPosition.FileInfo.FullName : string.Empty;
            Match match = new Regex(@"\\src\\(.*)\\").Match(sourceFilePath);
            if (!match.Success) return "misc";

            string path = match.Groups[1].Value;
            if (path.StartsWith("moai-ios")) return "moai-ios";
            if (path.StartsWith("moai-android")) return "moai-android";
            if (path.StartsWith("moai-fmod")) return "moai-fmod";
            return path;
        }

        public static FileInfo GetFileInfo(this MoaiClass moaiClass, DirectoryInfo outputRootDirectory) {
            return outputRootDirectory.GetDirectoryInfo(moaiClass.GetDirectoryString()).GetFileInfo(moaiClass.GetFileName());
        }

        public static string GetLinkTo(this MoaiClass source, MoaiClass target) {
            if (source == target) {
                // Link to this. Return formatted class name instead.
                return string.Format("`{0}`", target.Name);
            }

            // Calculate relative path
            string[] sourcePathElements = source.GetDirectoryString().Split('\\');
            string[] targetPathElements = target.GetDirectoryString().Split('\\');
            int commonElementCount = sourcePathElements
                .Zip(targetPathElements, (a, b) => new { a, b })
                .TakeWhile(pair => pair.a == pair.b)
                .Count();
            StringBuilder path = new StringBuilder();
            for (int depth = sourcePathElements.Length; depth > commonElementCount; depth--) {
                path.Append("../");
            }
            foreach (string element in targetPathElements.Skip(commonElementCount)) {
                path.AppendFormat("{0}/", element);
            }
            path.AppendFormat("{0}.md", target.Name);
            return string.Format("[{0}]({1})", target.Name, path);
        }
    }
}