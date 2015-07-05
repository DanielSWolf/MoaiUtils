using System;
using System.IO;
using System.Reflection;
using Antlr4.Runtime;

namespace CppParser {

	public class CodePosition {

		private readonly Lazy<TextPosition> textPosition;

		public CodePosition(AntlrInputStream inputStream, int charIndex) {
			if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
			if (charIndex < 0 || charIndex >= inputStream.Size) throw new ArgumentOutOfRangeException(nameof(inputStream));

			InputStream = inputStream;
			CharIndex = charIndex;
			textPosition = new Lazy<TextPosition>(() => {
				// Get data via reflection to prevent creating a (possibly long) temporary string
				char[] data = (char[]) typeof(AntlrInputStream).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inputStream);

				// Determine line and column
				int lineNumber = 1;
				int columnNumber = 1;
				for (int index = 0; index < charIndex; index++) {
					if (data[index] == '\n') {
						lineNumber++;
						columnNumber = 1;
					} else if (data[index] == '\t') {
						const int tabSize = 4;
						columnNumber = columnNumber - ((columnNumber - 1) % tabSize) + tabSize;
					} else {
						columnNumber++;
					}
				}
				return new TextPosition { LineNumber = lineNumber, ColumnNumber = columnNumber };
			});
		}

		public AntlrInputStream InputStream { get; }
		public int CharIndex { get; }

		public FileInfo File => new FileInfo(InputStream.name);

		/// <summary>
		/// Line number, 1-based.
		/// </summary>
		public int LineNumber => textPosition.Value.LineNumber;

		/// <summary>
		/// Column number, 1-based. Assuming 4 columns per tab.
		/// </summary>
		public int ColumnNumber => textPosition.Value.ColumnNumber;

		public override string ToString() {
			return $"{File} ({LineNumber}, {ColumnNumber})";
		}

		private class TextPosition {

			public int LineNumber;
			public int ColumnNumber;

		}

	}

}