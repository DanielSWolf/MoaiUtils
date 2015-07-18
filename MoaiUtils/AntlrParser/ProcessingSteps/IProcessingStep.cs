using System;
using System.Collections.Generic;
using CppParser.CodeIssues;

namespace CppParser.ProcessingSteps {

	public interface IProcessingStep {

		void Run(IProgress<double> progress);
		IReadOnlyCollection<ICodeIssue> CodeIssues { get; }

	}

}