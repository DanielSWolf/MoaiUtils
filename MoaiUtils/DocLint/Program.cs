using System;
using System.IO;
using System.Linq;
using MoaiUtils.Common;
using MoaiUtils.MoaiParsing;

namespace MoaiUtils.DocLint {
	internal class Program {
		private static int Main(string[] args) {
			return Bootstrapper.Start<Configuration>(args, Main);
		}

		private static void Main(Configuration configuration) {
			// Parse Moai code
			var parser = new MoaiParser(statusCallback: s => Console.WriteLine("[] {0}", s));
			parser.Parse(new DirectoryInfo(configuration.InputDirectory));

			// Show warnings
			Console.WriteLine();
			var orderedWarnings = parser.Warnings
				.OrderBy(warning => warning.Position.FileInfo.FullName);
			foreach (var warning in orderedWarnings) {
				Console.WriteLine("[{0}]\t{1}\t[{2}]", warning.Position.ToString(configuration.PathsAsUri), warning.Message, warning.Type);
			}
			Console.WriteLine("\n{0} warnings.", parser.Warnings.Count);
		}
	}
}