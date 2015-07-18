using System.IO;
using Antlr4.Runtime;

namespace CppParser.FileStreams {

	public abstract class FileStreamBase : AntlrInputStream {

		protected FileStreamBase(FileInfo file) {
			string text = File.ReadAllText(file.FullName);
			text = Preprocess(text);

			name = file.FullName;
			data = text.ToCharArray();
			n = text.Length;
		}

		public char this[int index] => data[index];

		protected abstract string Preprocess(string text);

		/// <summary>
		/// Converts all line breaks to "\n"s. This lets us deal with clean \n line breaks throughout.
		/// </summary>
		protected string CleanLineBreaks(string text) {
			return text.Replace("\r", "");
		}

	}

}