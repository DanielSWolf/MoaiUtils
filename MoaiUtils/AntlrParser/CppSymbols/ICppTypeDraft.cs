namespace CppParser.CppSymbols {

	/// <summary>
	/// A "rough version" of a C++ type before it is resolved to an actual <see cref="ICppType"/> instance.
	/// </summary>
	public interface ICppTypeDraft : ICodeElement {

		string Name { get; }

	}

}