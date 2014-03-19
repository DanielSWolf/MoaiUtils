using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatLuaMethodsAreDocumented : CheckBase {
        public override void Run() {
            IEnumerable<FileInfo> codeFiles = MoaiSrcDirectory.GetFilesRecursively(".cpp", ".h");

            foreach (var codeFile in codeFiles) {
                string code = File.ReadAllText(codeFile.FullName);
                WarnForUndocumentedLuaMethods(code, new FilePosition(codeFile));
            }
        }

        private static readonly Regex undocumentedLuaMethodRegex = new Regex(@"
            # No documentation preceding
            (?<!\*/\s*)
            # Lua method definition
            int\s+(?<className>[A-Za-z0-9_]+)\s*::\s*(?<methodName>_[A-Za-z0-9_]+)\s*\(
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private void WarnForUndocumentedLuaMethods(string code, FilePosition filePosition) {
            var matches = undocumentedLuaMethodRegex.Matches(code);
            foreach (Match match in matches) {
                string typeName = match.Groups["className"].Value;
                string methodName = match.Groups["methodName"].Value;
                MethodPosition methodPosition = new MethodPosition(new TypePosition(filePosition, typeName), methodName);
                Warnings.Add(methodPosition, WarningType.MissingAnnotation,
                    "Missing method documentation.");
            }
        }

    }
}