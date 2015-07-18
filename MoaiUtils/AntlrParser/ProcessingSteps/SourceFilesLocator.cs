using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using CppParser.CodeIssues;
using CppParser.FileStreams;
using MoaiUtils.Tools;

namespace CppParser.ProcessingSteps {

	public class SourceFilesLocator : IProcessingStep {

		private readonly DirectoryInfo sourceDir;
		private readonly List<ICodeIssue> codeIssues = new List<ICodeIssue>();

		public SourceFilesLocator(DirectoryInfo sourceDir) {
			this.sourceDir = sourceDir;
		}

		public IReadOnlyList<ICharStream> CppFileStreams { get; private set; }
		public IReadOnlyList<ICharStream> LuaFileStreams { get; private set; }
		public IReadOnlyCollection<ICodeIssue> CodeIssues => codeIssues;

		public void Run(IProgress<double> progress) {
			// Find all C++ files, ordered by full name
			string[] cppExtensions = { ".h", ".cpp", ".m", ".mm" };
			var cppFileInfos = sourceDir.EnumerateFiles("*.*", SearchOption.AllDirectories)
				.Where(file => cppExtensions.Contains(file.Extension.ToLowerInvariant()))
				.OrderBy(file => file.FullName)
				.ToList();
			CppFileStreams = cppFileInfos
				.Select(fileInfo => new CppFileStream(fileInfo), progress)
				.ToList();

			// Find moai.lua
			FileInfo moaiLua = sourceDir.GetFileInfo(@"lua-headers\moai.lua");
			if (moaiLua.Exists) {
				LuaFileStreams = new[] { new LuaFileStream(moaiLua) };
			} else {
				LuaFileStreams = new CppFileStream[0];
				codeIssues.Add(new UnexpectedFileStructureCodeIssue($"File '{moaiLua}' does not exist."));
			}
		}

	}

}