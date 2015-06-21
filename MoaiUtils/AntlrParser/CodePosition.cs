using System.IO;
using Antlr4.Runtime;

namespace CppParser {

	public class CodePosition {

		public CodePosition(FileInfo file, int lineNumber, int columnNumber) {
			File = file;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
		}

		public CodePosition(ParserRuleContext parserRuleContext) {
			IToken token = parserRuleContext.Start;
			File = new FileInfo(token.InputStream.SourceName);
			LineNumber = token.Line;
			ColumnNumber = token.Column + 1;
		}

		public FileInfo File { get; private set; }

		/// <summary>
		/// The line number, one-based
		/// </summary>
		public int LineNumber { get; private set; }

		/// <summary>
		/// The character index within the line, one-based
		/// </summary>
		public int ColumnNumber { get; private set; }

		public override string ToString() {
			return $"{File}({LineNumber},{ColumnNumber})";
		}

	}

}