using System;
using System.Collections.Generic;
using System.IO;
using CppParser.CodeIssues;

namespace CppParser {

	internal static class Program {

		private static void Main(string[] args) {
			var codeIssues = new List<ICodeIssue>();

			var sourceDir = new DirectoryInfo(@"X:\dev\projects\moai-dev\src");
			SourceFilesLocator sourceFilesLocator = new SourceFilesLocator(sourceDir);
			Process(sourceFilesLocator, codeIssues, "Reading source files from disk...");

			ParseTreesCreator parseTreesCreator = new ParseTreesCreator(sourceFilesLocator.CppFileStreams, sourceFilesLocator.LuaFileStreams);
			Process(parseTreesCreator, codeIssues, "Parsing files...");

			TypesExtractor typesExtractor = new TypesExtractor(parseTreesCreator.CppParseTrees, parseTreesCreator.LuaParseTrees);
			Process(typesExtractor, codeIssues, "Performing code analysis...");

			Console.WriteLine();

			// Output code issues in Visual Studio format
			foreach (ICodeIssue codeIssue in codeIssues) {
				Console.WriteLine(FormatVisualStudioWarning(codeIssue));
			}
		}

		private static void Process(IProcessingStep processingStep, List<ICodeIssue> codeIssues, string message) {
			const int leftColumnWidth = 35;

			Console.Write(message.PadRight(leftColumnWidth));
			DateTime start = DateTime.Now;
			using (var progress = new ProgressBar()) {
				processingStep.Run(progress);
				codeIssues.AddRange(processingStep.CodeIssues);
			}
			Console.WriteLine("Done ({0:f3}s).", (DateTime.Now - start).TotalSeconds);
		}

		private static string FormatVisualStudioWarning(ICodeIssue codeIssue) {
			var pos = codeIssue.Position;
			string file = pos.ColumnNumber != null ? $"{pos.File}({pos.LineNumber},{pos.ColumnNumber})"
				: $"{pos.File}({pos.LineNumber ?? 1})";
			string errorCode = codeIssue.GetType().Name.Replace("CodeIssue", string.Empty);
			return $"{file} : warning {errorCode} : {codeIssue.Message}";
		}

	}

}