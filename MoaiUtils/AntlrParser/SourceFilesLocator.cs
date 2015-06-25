using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using MoaiUtils.Tools;

namespace CppParser {

	public class SourceFilesLocator : IProcessingStep {

		private readonly DirectoryInfo sourceDir;

		public SourceFilesLocator(DirectoryInfo sourceDir) {
			this.sourceDir = sourceDir;
		}

		public IReadOnlyList<ICharStream> CppFileStreams { get; private set; }
		public IReadOnlyList<ICharStream> LuaFileStreams { get; private set; }

		public void Run(IProgress<double> progress) {
			// Find all C++ files
			string[] cppExtensions = { ".h", ".cpp", ".m", ".mm" };
			var cppFileInfos = sourceDir.EnumerateFiles("*.*", SearchOption.AllDirectories)
				.Where(file => cppExtensions.Contains(file.Extension.ToLowerInvariant()))
				.ToList();
			CppFileStreams = cppFileInfos
				.Select(fileInfo => new CppFileStream(fileInfo), progress)
				.ToList();

			// Find moai.lua
			// TODO: Use better-suited stream
			LuaFileStreams = new[] { new CppFileStream(sourceDir.GetFileInfo(@"lua-headers\moai.lua")) };
		}

	}

}