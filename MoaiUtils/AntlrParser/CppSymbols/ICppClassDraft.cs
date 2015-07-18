using System.Collections.Generic;

namespace CppParser.CppSymbols {

	public interface ICppClassDraft : ICppTypeDraft, IContextProvider<CppParser.ClassDefinitionContext> {

		IReadOnlyCollection<string> BaseTypeNames { get; }

	}

}