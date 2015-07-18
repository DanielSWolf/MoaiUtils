using System.IO;

namespace CppParser.FileStreams {

	public class LuaFileStream : FileStreamBase {

		public LuaFileStream(FileInfo file) : base(file) {}

		protected override string Preprocess(string text) {
			return CleanLineBreaks(text);
		}

	}

}