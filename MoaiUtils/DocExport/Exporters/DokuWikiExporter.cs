using System;
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
    public class DokuWikiExporter : IApiExporter {
        public void Export(MoaiClass[] classes, string header, DirectoryInfo outputDirectory) {
            // Create info records for all classes
            var classInfoByName = new Dictionary<string, ClassInfo>();
            foreach (MoaiClass moaiClass in classes) {
                string filePath = moaiClass.ClassPosition.FileInfo.FullName;
                if (!filePath.Contains(@"\src\")) continue;
                classInfoByName[moaiClass.Name] = GetClassInfo(moaiClass, outputDirectory);
            }

            foreach (ClassInfo typeInfo in classInfoByName.Values) {
                ExportClass(typeInfo, classInfoByName);
            }
        }

        private void ExportClass(ClassInfo classInfo, Dictionary<string, ClassInfo> classInfoByName) {
            Func<string, string> Format = s => this.Format(s, classInfo, classInfoByName);
            classInfo.FileInfo.Directory.Create();
            using (var file = classInfo.FileInfo.CreateText()) {
                MoaiClass moaiClass = classInfo.MoaiClass;
                file.WriteLine("====== {0} ======\n", moaiClass.Name);
                var baseClasses = moaiClass.BaseClasses.Where(c => c.Name != "MOAILuaObject").ToArray();
                if (baseClasses.Any()) {
                    file.WriteLine("class ''{0}'' : {1}\n", moaiClass.Name, Format(baseClasses.Select(c => c.Name).Join(", ")));
                }
                if (moaiClass.Description != null) {
                    file.WriteLine(Format(moaiClass.Description.SurroundLines("//", "//")));
                }

                var fields = moaiClass.Members.OfType<Field>().ToArray();
                if (fields.Any()) {
                    file.WriteLine();
                    file.WriteLine("===== Fields =====\n");
                    foreach (Field field in fields) {
                        file.WriteLine("  * ''{0}''", field.Name);
                        if (field.Description != null) {
                            file.WriteLine(Format(field.Description).SurroundLines("      * //", "//"));
                        }
                    }
                }

                var methods = moaiClass.Members.OfType<Method>().ToArray();
                if (methods.Any()) {
                    file.WriteLine();
                    file.WriteLine("===== Methods =====\n");
                    foreach (Method method in methods) {
                        string signature = String.Format("{0} **{1}** {2}",
                            method.OutParameterSignature != null ? method.OutParameterSignature.ToString(SignatureGrouping.Any) : "?",
                            method.Name,
                            method.InParameterSignature != null ? method.InParameterSignature.ToString(SignatureGrouping.Parentheses) : "?");
                        file.WriteLine("''{0}''", Format(signature));
                        if (method.Description != null) {
                            file.WriteLine("\n" + Format(method.Description).SurroundLines("> "));
                        }
                        file.WriteLine("\n----\n");
                    }
                }
            }
        }

        private class ClassInfo {
            public MoaiClass MoaiClass { get; set; }
            public FileInfo FileInfo { get; set; }
            public string DirectoryIdentifier { get; set; }
            public string FileIdentifier { get; set; }
        }

        private ClassInfo GetClassInfo(MoaiClass moaiClass, DirectoryInfo outputDirectory) {
            string filePath = moaiClass.ClassPosition.FileInfo.FullName;
            string directory = ToWikiIdentifier(new Regex(@"\\src\\(.*)\\").Match(filePath).Groups[1].Value);
            string file = ToWikiIdentifier(moaiClass.Name);

            return new ClassInfo {
                MoaiClass = moaiClass,
                FileInfo = outputDirectory.GetDirectoryInfo(directory).GetFileInfo(file + ".txt"),
                DirectoryIdentifier = directory,
                FileIdentifier = file
            };
        }

        private string ToWikiIdentifier(string s) {
            StringBuilder result = new StringBuilder();
            bool upperCaseNeedsSeparator = false;
            foreach (char c in s) {
                bool charIsUpper = char.IsUpper(c);
                if (charIsUpper && upperCaseNeedsSeparator) {
                    result.Append('_');
                }
                result.Append(char.ToLower(c));
                upperCaseNeedsSeparator = (!charIsUpper && !char.IsNumber(c)) || result.ToString() == "moai";
            }

            return result.ToString();
        }

        private static readonly Regex identifierRegex = new Regex(@"(?<![A-Za-z0-9_])[A-Za-z0-9_]+");

        private string Format(string s, ClassInfo contextClassInfo, Dictionary<string, ClassInfo> classInfoByName) {
            s = s ?? string.Empty;

            //// Escape special sequences
            //string[] specialSequences = { "__", "''", "[[", "]]" };
            //foreach (string specialSequence in specialSequences) {
            //    s = s.Replace(specialSequence, "%%" + specialSequence + "%%");
            //}

            // Replace class names with formatted internal links
            s = identifierRegex.Replace(s, match => {
                if (!classInfoByName.ContainsKey(match.Value)) return match.Value;
                var classInfo = classInfoByName[match.Value];
                if (classInfo == contextClassInfo) return match.Value;
                string link = (classInfo.DirectoryIdentifier == contextClassInfo.DirectoryIdentifier)
                    ? classInfo.FileIdentifier
                    : String.Format(":{0}:{1}", classInfo.DirectoryIdentifier, classInfo.FileIdentifier);
                return String.Format("''[[{0}|{1}]]''", link, match.Value);
            });

            // Duplicate line breaks
            s = s.Replace("\r\n", "\r\n\r\n");

            return s;
        }
    }

    public static class StringExtensions {
        public static string EscapeLines(this string s) {
            return @"\\ " + s.Replace("\r\n", @"\\ ");
        }
    }

}