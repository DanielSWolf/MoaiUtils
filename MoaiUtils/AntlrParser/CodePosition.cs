using System;
using System.IO;
using Antlr4.Runtime;

namespace CppParser {

	public class CodePosition {

		public static readonly CodePosition None = new CodePosition(new FileInfo("No file"));

		public CodePosition(FileInfo file, int? lineNumber = null, int? columnNumber = null) {
			if (file == null) throw new ArgumentNullException(nameof(file));
			if (columnNumber == null && lineNumber != null) throw new ArgumentNullException(nameof(columnNumber));

			File = file;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
		}

		public CodePosition(IToken token) {
			File = new FileInfo(token.InputStream.SourceName);
			LineNumber = token.Line;
			ColumnNumber = token.Column + 1;
		}

		public CodePosition(ParserRuleContext parserRuleContext) : this(parserRuleContext.Start) { }

		public FileInfo File { get; }

		/// <summary>
		/// The line number, one-based
		/// </summary>
		public int? LineNumber { get; }

		/// <summary>
		/// The character index within the line, one-based
		/// </summary>
		public int? ColumnNumber { get; }

		public override string ToString() {
			return ColumnNumber != null ? $"{File} ({LineNumber}, {ColumnNumber})"
				: LineNumber != null ? $"{File} ({LineNumber})"
				: File.ToString();
		}

	}

}