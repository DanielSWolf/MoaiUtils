namespace MoaiUtils.MoaiParsing.Parsing {

	/// <summary>
	/// A primitive recursive descent parser to find matching braces
	/// </summary>
	public static class BlockParser {
		public static int GetBlockLength(string code, int openingBraceIndex) {
			int index = openingBraceIndex + 1;
			while (index < code.Length) {
				if (code[index] == '"') {
					// Beginning of string
					index += GetStringLiteralLength(code, index);
				} else if (index + 1 < code.Length && code.Substring(index, 2) == "//") {
					// Beginning of single-line comment
					index += GetSingleLineCommentLength(code, index);
				} else if (index + 1 < code.Length && code.Substring(index, 2) == "/*") {
					// Beginning of multi-line comment
					index += GetMultiLineCommentLength(code, index);
				} else if (code[index] == '{') {
					// Beginning of nested block
					index += GetBlockLength(code, index);
				} else if (code[index] == '}') {
					// End of this block
					return index - openingBraceIndex + 1;
				} else {
					// Any regular character
					index++;
				}
			}

			// Syntax error
			return index - openingBraceIndex;
		}

		private static int GetStringLiteralLength(string code, int doubleQuoteIndex) {
			int index = doubleQuoteIndex;
			while (index < code.Length) {
				if (code[index] == '"') return index - doubleQuoteIndex + 1;
				index += (code[index] == '\\') ? 2 : 1;
			}

			// Syntax error
			return index - doubleQuoteIndex;
		}

		private static int GetSingleLineCommentLength(string code, int firstDashIndex) {
			int index = firstDashIndex;
			while (index < code.Length && code[index] != '\n' && code[index] != '\r') index++;
			return index - firstDashIndex;
		}

		private static int GetMultiLineCommentLength(string code, int initialDashIndex) {
			int index = initialDashIndex;
			while (index + 1 < code.Length) {
				if (code.Substring(index, 2) == "*/") return index - initialDashIndex + 2;
				index++;
			}

			// Syntax error
			return index - initialDashIndex;
		}
	}

}