namespace CppParser.CppSymbols {

	public interface ICppTypedefDraft : ICppTypeDraft, IContextProvider<CppParser.TypedefContext> {

		string TargetName { get; }
		bool IsFunction { get; }
		bool IsPointer { get; }

	}

}