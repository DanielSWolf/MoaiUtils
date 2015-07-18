using System;
using System.IO;
using CppParser.FileStreams;

namespace CppParser {

	public class CodePosition : IComparable {

		private readonly FileStreamBase inputStream;
		private readonly Lazy<TextPosition> textPosition;

		public CodePosition(FileStreamBase inputStream, int charIndex) {
			if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
			if (charIndex < 0 || charIndex >= inputStream.Size) throw new ArgumentOutOfRangeException(nameof(inputStream));

			this.inputStream = inputStream;
			CharIndex = charIndex;
			textPosition = new Lazy<TextPosition>(() => {
				// Determine line and column
				int lineNumber = 1;
				int charNumber = 1;
				int columnNumber = 1;
				for (int index = 0; index < charIndex; index++) {
					if (inputStream[index] == '\n') {
						lineNumber++;
						charNumber = 1;
						columnNumber = 1;
					} else if (inputStream[index] == '\t') {
						const int tabSize = 4;
						charNumber++;
						columnNumber = columnNumber - ((columnNumber - 1) % tabSize) + tabSize;
					} else {
						charNumber++;
						columnNumber++;
					}
				}
				return new TextPosition { LineNumber = lineNumber, CharNumber = charNumber, ColumnNumber = columnNumber };
			});
		}

		public FileInfo File => new FileInfo(inputStream.name);

		public int CharIndex { get; }

		/// <summary>
		/// Line number, 1-based.
		/// </summary>
		public int LineNumber => textPosition.Value.LineNumber;

		/// <summary>
		/// Column number within the text line, 1-based. Assuming 4 columns per tab.
		/// </summary>
		public int LineColumnNumber => textPosition.Value.ColumnNumber;

		/// <summary>
		/// Character number within the text line, 1-based. A tab counts as 1 character.
		/// </summary>
		public int LineCharNumber => textPosition.Value.CharNumber;

		public override string ToString() {
			return $"{File} ({LineNumber}, {LineColumnNumber})";
		}

		#region Equality and comparison

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			CodePosition other = (CodePosition) obj;
			return Equals(inputStream, other.inputStream) && CharIndex == other.CharIndex;
		}

		public override int GetHashCode() {
			unchecked {
				return (inputStream.GetHashCode() * 397) ^ CharIndex;
			}
		}

		public int CompareTo(object obj) {
			CodePosition other = (CodePosition) obj;
			if (inputStream != other.inputStream) return inputStream.name.CompareTo(other.inputStream.name);
			return CharIndex.CompareTo(other.CharIndex);
		}

		#endregion

		private class TextPosition {

			public int LineNumber;
			public int CharNumber;
			public int ColumnNumber;

		}

	}

}