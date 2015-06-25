using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using MoaiUtils.Tools;

namespace CppParser {

	public class ParseTreesCreator : IProcessingStep {

		private readonly IReadOnlyList<ICharStream> cppFileStreams;
		private readonly IReadOnlyList<ICharStream> luaFileStreams;

		public ParseTreesCreator(IReadOnlyList<ICharStream> cppFileStreams, IReadOnlyList<ICharStream> luaFileStreams) {
			this.cppFileStreams = cppFileStreams;
			this.luaFileStreams = luaFileStreams;
		}

		public IReadOnlyList<CppParser.FileContext> CppParseTrees { get; private set; }
		public IReadOnlyList<LuaParser.ChunkContext> LuaParseTrees { get; private set; }
		public int ErrorCount { get; private set; }

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
			lexer.AddErrorListener(new LexerDebugErrorListener(() => ErrorCount++));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			CppParser parser = new CppParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(() => ErrorCount++));

			return parser.file();
		}

		private LuaParser.ChunkContext ParseLuaFile(ICharStream charStream) {
			LuaLexer lexer = new LuaLexer(charStream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new LexerDebugErrorListener(() => ErrorCount++));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			LuaParser parser = new LuaParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(() => ErrorCount++));

			return parser.chunk();
		}

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

			public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState) { }
		}

	}

}