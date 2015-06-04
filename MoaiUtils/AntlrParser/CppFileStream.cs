using System.IO;
using System.Text;
using Antlr4.Runtime;

namespace CppParser {
    public class CppFileStream : AntlrInputStream {
        public CppFileStream(FileInfo file) : base(Preprocess(File.ReadAllText(file.FullName))) {
            name = file.FullName;
        }

        private static string Preprocess(string code) {

            // Remove carriage returns. This lets us deal with clean \n line breaks throughout.
            code = code.Replace("\r", "");

            StringBuilder result = new StringBuilder();

            // Remove backshashes followed by line breaks. They should be treated as a single line.
            int suppressedLineBreaks = 0;
            for (int i = 0; i < code.Length; i++) {
                char current = code[i];
                char next = i + 1 < code.Length ? code[i + 1] : '\0';

                if (current == '\\' && next == '\n') {
                    i++;
                    suppressedLineBreaks++;
                    continue;
                }

                // If we've suppressed line breaks: Add them once the logical line is finished to keep line numbers correct
                if (current == '\n' && suppressedLineBreaks > 0) {
                    result.Append('\n', suppressedLineBreaks);
                    suppressedLineBreaks = 0;
                }

                result.Append(current);
            }

            return result.ToString();
        }
    }
}