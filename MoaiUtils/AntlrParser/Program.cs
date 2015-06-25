using System;
using System.IO;

namespace CppParser {

	internal static class Program {

		private static void Main(string[] args) {
			const int leftColumnWidth = 35;

			var sourceDir = new DirectoryInfo(@"X:\dev\projects\moai-dev\src");

			Console.Write("Reading source files from disk...".PadRight(leftColumnWidth));
			SourceFilesLocator sourceFilesLocator = new SourceFilesLocator(sourceDir);
			using (var progress = new ProgressBar()) {
				sourceFilesLocator.Run(progress);
			}
			Console.WriteLine("Done.");

			Console.Write("Parsing files...".PadRight(leftColumnWidth));
			ParseTreesCreator parseTreesCreator = new ParseTreesCreator(sourceFilesLocator.CppFileStreams, sourceFilesLocator.LuaFileStreams);
			using (var progress = new ProgressBar()) {
				parseTreesCreator.Run(progress);
			}
			Console.WriteLine("Done with {0} errors.", parseTreesCreator.ErrorCount);

			Console.Write("Performing code analysis...".PadRight(leftColumnWidth));
			TypesExtractor typesExtractor = new TypesExtractor(parseTreesCreator.CppParseTrees, parseTreesCreator.LuaParseTrees);
			using (var progress = new ProgressBar()) {
				typesExtractor.Run(progress);
			}
			Console.WriteLine("Done.");

			Console.ReadLine();
		}

	}

}