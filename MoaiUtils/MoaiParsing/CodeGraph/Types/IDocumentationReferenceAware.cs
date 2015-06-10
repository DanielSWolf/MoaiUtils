using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing.CodeGraph.Types {

	public interface IDocumentationReferenceAware {
		IEnumerable<FilePosition> DocumentationReferences { get; }
		void AddDocumentationReference(FilePosition position);
	}

}