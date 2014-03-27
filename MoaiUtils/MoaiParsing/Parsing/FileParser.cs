using System.IO;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class FileParser {

        public static void ParseMoaiCodeFile(FileInfo codeFile, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            string code = codeFile.ReadAllText();
            ParseMoaiCodeFile(code, filePosition, types, warnings);
        }

        public static void ParseMoaiCodeFile(string code, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            ClassParser.ParseClassDefinitions(code, filePosition, types, warnings);
            MethodParser.ParseMethodDefinitions(code, filePosition, types, warnings);
        }

    }
}