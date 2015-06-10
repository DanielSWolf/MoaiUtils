using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using Antlr4.Runtime.Tree;
using MoaiUtils.Tools;

namespace CppParser {

	internal class Program {

		private class LexerDebugErrorListener : IAntlrErrorListener<int> {
			private readonly Action addError;

			public LexerDebugErrorListener(Action addError) {
				this.addError = addError;
			}

			public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
				addError();
			}
		}

		private class DebugErrorListener : BaseErrorListener {
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

			public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState) {}
		}

		private static void Main(string[] args) {
			string[] cppExtensions = {".h", ".cpp", ".m", ".mm"};
			var sourceDir = new DirectoryInfo(@"X:\dev\projects\moai-dev\src");
			var files = sourceDir.EnumerateFiles("*.*", SearchOption.AllDirectories)
				.Where(file => cppExtensions.Contains(file.Extension.ToLowerInvariant()))
				.ToList();

			int errorCount = 0;
			IntProgress progress = new IntProgress {MaxValue = files.Count + 1};
			Console.Write("Parsing files... ");
			using (new ProgressBar(progress)) {
				{
					// TODO: Use better-suited stream
					ICharStream charStream = new CppFileStream(sourceDir.GetFileInfo(@"lua-headers\moai.lua"));
					LuaLexer lexer = new LuaLexer(charStream);
					lexer.RemoveErrorListeners();
					lexer.AddErrorListener(new LexerDebugErrorListener(() => errorCount++));
					CommonTokenStream tokenStream = new CommonTokenStream(lexer);
					LuaParser parser = new LuaParser(tokenStream);
					parser.RemoveErrorListeners();
					parser.AddErrorListener(new DebugErrorListener(() => errorCount++));

					IParseTree parseTree = parser.chunk();

					progress.Value++;
				}

				List<CppParser.FileContext> fileContexts = new List<CppParser.FileContext>();
				foreach (FileInfo file in files) {
					CppParser.FileContext fileContext = ParseCppFile(file, () => errorCount++);
					fileContexts.Add(fileContext);

					progress.Value++;
				}
			}

			Console.WriteLine("Done with {0} errors.", errorCount);
			Console.ReadLine();
		}

		private static CppParser.FileContext ParseCppFile(FileInfo file, Action addError) {
			ICharStream charStream = new CppFileStream(file);
			CppLexer lexer = new CppLexer(charStream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new LexerDebugErrorListener(addError));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			CppParser parser = new CppParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(addError));

			return parser.file();
		}
	}

}