using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppParser.CodeIssues;
using CppParser.ProcessingSteps;

namespace CppParser {

	internal static class Program {

		private static void Main(string[] args) {
			var codeIssues = new List<ICodeIssue>();

			// TODO: Read from command line
			var sourceDir = new DirectoryInfo(@"X:\dev\projects\moai-dev\src");
			SourceFilesLocator sourceFilesLocator = new SourceFilesLocator(sourceDir);
			Process(sourceFilesLocator, codeIssues, "Reading source files from disk...");

			ParseTreesCreator parseTreesCreator = new ParseTreesCreator(sourceFilesLocator.CppFileStreams, sourceFilesLocator.LuaFileStreams);
			Process(parseTreesCreator, codeIssues, "Parsing files...");

			CppTypesExtractor cppTypesExtractor = new CppTypesExtractor(parseTreesCreator.CppParseTrees);
			Process(cppTypesExtractor, codeIssues, "Performing code analysis...");

			Console.WriteLine();

			// TODO: Remove debug output
			// Output code issues in Visual Studio format
			foreach (ICodeIssue codeIssue in codeIssues) {
				Console.WriteLine(FormatVisualStudioWarning(codeIssue));
			}
			// Write symbols to file
			using (var file = File.CreateText(@"C:\Users\Daniel\Desktop\tmp.txt")) {
				foreach (var pair in cppTypesExtractor.Types.OrderBy(pair => pair.Value.ToString()).ThenBy(pair => pair.Key)) {
					file.WriteLine("{0}\t{1}", pair.Key, pair.Value);
				}
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
			string file = $"{pos.File}({pos.LineNumber},{pos.LineCharNumber})";
			string errorCode = codeIssue.GetType().Name.Replace("CodeIssue", string.Empty);
			return $"{file} : warning {errorCode} : {codeIssue.Message}";
		}

	}

}