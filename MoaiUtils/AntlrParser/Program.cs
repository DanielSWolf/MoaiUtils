using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using Antlr4.Runtime.Tree;

namespace CppParser {
    class Program {

        class LexerDebugErrorListener : IAntlrErrorListener<int> {
            private readonly Action addError;

            public LexerDebugErrorListener(Action addError) {
                this.addError = addError;
            }

            public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
                addError();
            }
        }

        class DebugErrorListener : BaseErrorListener {
            private readonly Action addError;

            public DebugErrorListener(Action addError) {
                this.addError = addError;
            }

            public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
                addError();
            }

            public override void ReportAmbiguity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, bool exact, BitSet ambigAlts, ATNConfigSet configs) {
                addError();
            }

            public override void ReportAttemptingFullContext(Parser recognizer, DFA dfa, int startIndex, int stopIndex, BitSet conflictingAlts, SimulatorState conflictState) {
                addError();
            }

            public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState) {
            }
        }

        static void Main(string[] args) {
            string[] cppExtensions = { ".h", ".cpp", ".m", ".mm" };
            var files = new DirectoryInfo(@"X:\dev\projects\moai-dev\src").EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Where(file => cppExtensions.Contains(file.Extension.ToLowerInvariant()))
                .ToList();

            int errorCount = 0;
            IntProgress progress = new IntProgress { MaxValue = files.Count };
            Console.Write("Parsing files... ");
            using (new ProgressBar(progress)) {
                foreach (FileInfo file in files) {
                    ICharStream charStream = new CppFileStream(file);
                    MoaiCppLexer lexer = new MoaiCppLexer(charStream);
                    lexer.RemoveErrorListeners();
                    lexer.AddErrorListener(new LexerDebugErrorListener(() => errorCount++));
                    CommonTokenStream tokenStream = new CommonTokenStream(lexer);
                    MoaiCppParser parser = new MoaiCppParser(tokenStream);
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new DebugErrorListener(() => errorCount++));

                    IParseTree parseTree = parser.file();

                    progress.Value++;
                }
            }

            Console.WriteLine("Done with {0} errors.", errorCount);
            Console.ReadLine();
        }
    }
}
