using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using CppParser.CodeIssues;
using MoaiUtils.Tools;

namespace CppParser {

	public class ParseTreesCreator : IProcessingStep {

		private readonly IReadOnlyList<ICharStream> cppFileStreams;
		private readonly IReadOnlyList<ICharStream> luaFileStreams;
		private readonly List<ICodeIssue> codeIssues = new List<ICodeIssue>();

		public ParseTreesCreator(IReadOnlyList<ICharStream> cppFileStreams, IReadOnlyList<ICharStream> luaFileStreams) {
			this.cppFileStreams = cppFileStreams;
			this.luaFileStreams = luaFileStreams;
		}

		public IReadOnlyList<CppParser.FileContext> CppParseTrees { get; private set; }
		public IReadOnlyList<LuaParser.ChunkContext> LuaParseTrees { get; private set; }
		public IReadOnlyCollection<ICodeIssue> CodeIssues => codeIssues;

		public void Run(IProgress<double> progress) {
			// Parse C++ files
			CppParseTrees = cppFileStreams
				.Select(ParseCppFile, progress)
				.ToList();

			// Parse Lua files
			LuaParseTrees = luaFileStreams
				.Select(ParseLuaFile)
				.ToList();
		}

		private CppParser.FileContext ParseCppFile(ICharStream charStream) {
			CppLexer lexer = new CppLexer(charStream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new LexerDebugErrorListener(codeIssues));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			CppParser parser = new CppParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(codeIssues));

			return parser.file();
		}

		private LuaParser.ChunkContext ParseLuaFile(ICharStream charStream) {
			LuaLexer lexer = new LuaLexer(charStream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new LexerDebugErrorListener(codeIssues));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			LuaParser parser = new LuaParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(codeIssues));

			return parser.chunk();
		}

		private class LexerDebugErrorListener : IAntlrErrorListener<int> {

			private readonly List<ICodeIssue> codeIssues;

			public LexerDebugErrorListener(List<ICodeIssue> codeIssues) {
				this.codeIssues = codeIssues;
			}

			public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
				CodePosition codePosition = new CodePosition((AntlrInputStream) recognizer.InputStream, recognizer.InputStream.Index - 1);
				codeIssues.Add(new ParsingErrorCodeIssue(codePosition, msg));
			}
		}

		private class DebugErrorListener : BaseErrorListener {

			private readonly List<ICodeIssue> codeIssues;

			public DebugErrorListener(List<ICodeIssue> codeIssues) {
				this.codeIssues = codeIssues;
			}

			public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
				CodePosition codePosition = offendingSymbol.GetCodePosition();
				codeIssues.Add(new ParsingErrorCodeIssue(codePosition, msg));
			}

			public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState) { }
		}

	}

}