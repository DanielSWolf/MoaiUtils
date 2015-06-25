using System;

namespace CppParser {

	public interface IProcessingStep {

		void Run(IProgress<double> progress);

	}

}